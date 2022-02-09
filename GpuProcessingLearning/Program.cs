using ILGPU;

namespace GpuProcessingLearning;
public class Program
{
    public static void Main(string[] args)
    {
        using var context = Context.CreateDefault();

        foreach(var device in context)
        {
            using var accelerator = device.CreateAccelerator(context);
        }
    }
}

