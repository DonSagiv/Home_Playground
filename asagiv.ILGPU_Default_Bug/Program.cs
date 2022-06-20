using ILGPU.Runtime;
using ILGPU;
using System.Linq;
using System;
using System.Collections.Generic;
using Context = ILGPU.Context;

namespace asagiv.ILGPU_Default_Bug
{
    public class Program
    {
        #region Fields
        private static Context _context;
        private static Device _device;
        private static Accelerator _accelerator;
        private static MemoryBuffer2D<byte, Stride2D.DenseX> _inputFrameBuffer;
        private static MemoryBuffer2D<byte, Stride2D.DenseX> _subtractFrameBuffer;
        private static MemoryBuffer2D<byte, Stride2D.DenseX> _thresholdFrameBuffer;
        private static MemoryBuffer2D<int, Stride2D.DenseX> _sumStackBuffer;
        private static MemoryBuffer2D<int, Stride2D.DenseX> _hasPixelStackbuffer;
        private static MemoryBuffer1D<long, Stride1D.Dense> _valueBuffer;
        private static Action<Index2D, byte,
            ArrayView2D<byte, Stride2D.DenseX>,
            ArrayView2D<byte, Stride2D.DenseX>,
            ArrayView2D<byte, Stride2D.DenseX>,
            ArrayView2D<int, Stride2D.DenseX>,
            ArrayView2D<int, Stride2D.DenseX>,
            ArrayView1D<long, Stride1D.Dense>> _kernelAction;
        private static Queue<byte[,]> _frameQueue;
        #endregion

        #region Kernel
        private static void Kernel(Index2D i, byte threshold,
            ArrayView2D<byte, Stride2D.DenseX> inputFrameView,
            ArrayView2D<byte, Stride2D.DenseX> subtractFrameView,
            ArrayView2D<byte, Stride2D.DenseX> thresholdFrameView,
            ArrayView2D<int, Stride2D.DenseX> sumStackView,
            ArrayView2D<int, Stride2D.DenseX> hasPixelStackView,
            ArrayView1D<long, Stride1D.Dense> valueView)
        {
            // This is fixed by changing "default" to 0, but will leave for debugging puurposes.
            // Get the pixel value from each input and subtract frame.
            var add = inputFrameView[i] > threshold ? inputFrameView[i] : default;
            var subtract = subtractFrameView[i] > threshold ? subtractFrameView[i] : default;

            // 1 if input frame is above threshold, 0 if not.
            var densityAdd = inputFrameView[i] > threshold ? (byte)1 : default;
            var densitySubtract = subtractFrameView[i] > threshold ? (byte)1 : default;

            // Set the threshold frame pixel
            thresholdFrameView[i] = add;

            // Update the stack pixel
            sumStackView[i] += add - subtract;

            // Update the has pixel stack
            hasPixelStackView[i] += densityAdd - densitySubtract;

            // Update the total sum intensity (sum of all stacks' X and Y pixel values)
            Atomic.Add(ref valueView[0], add - subtract);

            // Update the total has pixel intensity (sum of all stacks' X and Y 0 and 1 values)
            Atomic.Add(ref valueView[1], densityAdd - densitySubtract);
        }
        #endregion

        #region Methods
        public static void Main(string[] args)
        {
            _frameQueue = new Queue<byte[,]>();

            _context = Context.CreateDefault();

            _device = _context.Devices.FirstOrDefault(x => x.AcceleratorType == AcceleratorType.OpenCL);

            _accelerator = _device.CreateAccelerator(_context);

            _kernelAction = _accelerator.LoadAutoGroupedStreamKernel<Index2D, byte,
                ArrayView2D<byte, Stride2D.DenseX>,
                ArrayView2D<byte, Stride2D.DenseX>,
                ArrayView2D<byte, Stride2D.DenseX>,
                ArrayView2D<int, Stride2D.DenseX>,
                ArrayView2D<int, Stride2D.DenseX>,
                ArrayView1D<long, Stride1D.Dense>>(Kernel);

            var inputFrame = new byte[800, 400]; // Frame that will be added to the stack
            var subtractFrame = new byte[800, 400]; // Frame that will be removed from the stack once the stack depth reaches a certain amount
            var thresholdFrame = new byte[800, 400]; // Thresholded input frame
            var sumStackFrame = new int[800, 400]; // The rolling stack
            var hasPixelStackFrame = new int[800, 400]; // Sum of detected pixels of each image in the stack
            var values = new long[2];

            _inputFrameBuffer = _accelerator.Allocate2DDenseX(inputFrame);
            _subtractFrameBuffer = _accelerator.Allocate2DDenseX(inputFrame);
            _thresholdFrameBuffer = _accelerator.Allocate2DDenseX(inputFrame);
            _sumStackBuffer = _accelerator.Allocate2DDenseX(sumStackFrame);
            _hasPixelStackbuffer = _accelerator.Allocate2DDenseX(sumStackFrame);
            _valueBuffer = _accelerator.Allocate1D(values);

            _sumStackBuffer.CopyFromCPU(sumStackFrame);
            _hasPixelStackbuffer.CopyFromCPU(hasPixelStackFrame);
            _thresholdFrameBuffer.CopyFromCPU(thresholdFrame);
            _valueBuffer.CopyFromCPU(values);

            var numImages = 300;

            for (var i = 0; i < numImages; i++)
            {
                Console.WriteLine($"Processing Frame {i + 1}");

                // Create random set of bytes to simulate a random set of pixels.
                var randomBytes = new byte[800 * 400];

                var rand = new Random();
                rand.NextBytes(randomBytes);

                var newInputFrame = new byte[800, 400];

                Buffer.BlockCopy(randomBytes, 0, newInputFrame, 0, inputFrame.Length);

                // Add the input frame to a queue
                _frameQueue.Enqueue(newInputFrame);

                if(i > 50)
                {
                    // Once 50 images are in the stack, get dequeue the frame to subtract.
                    subtractFrame = _frameQueue.Dequeue();

                    _subtractFrameBuffer.CopyFromCPU(subtractFrame);
                }

                _inputFrameBuffer.CopyFromCPU(newInputFrame);

                _kernelAction.Invoke(new Index2D(800, 400), 25,
                    _inputFrameBuffer,
                    _subtractFrameBuffer,
                    _thresholdFrameBuffer,
                    _sumStackBuffer,
                    _hasPixelStackbuffer,
                    _valueBuffer);

                _accelerator.Synchronize();

                _valueBuffer.CopyToCPU(values);

                // Get the thresholded frame after every operation.
                _thresholdFrameBuffer.CopyToCPU(thresholdFrame);
            }

            // Only get the stacks after the operation is completed.
            _sumStackBuffer.CopyToCPU(sumStackFrame);
            _hasPixelStackbuffer.CopyToCPU(hasPixelStackFrame);

            Console.WriteLine("*** Sum pixel average and total should be positive. ***");

            Console.WriteLine($"Average Sum Pixels= {GetAverage(sumStackFrame, numImages)}");
            Console.WriteLine($"Total Sum Pixels = {values[0]}");
            Console.WriteLine($"Average Has Pixels = {GetAverage(hasPixelStackFrame, numImages)}");
            Console.WriteLine($"Total Has Pixels = {values[1]}");

            Console.WriteLine("Analysis Complete");

            Console.ReadKey();
        }

        public static double GetAverage(int[,] stack, int stackSize)
        {
            int sum = 0;

            for (var i = 0; i < stack.GetLength(0); i++)
            {
                for (var j = 0; j < stack.GetLength(1); j++)
                {
                    sum += stack[i, j];
                }
            }

            return Convert.ToDouble(sum) / stackSize;
        }
        #endregion

    }
}
