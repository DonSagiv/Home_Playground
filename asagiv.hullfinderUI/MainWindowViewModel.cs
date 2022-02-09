using asagiv.mathcommon;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using SkiaSharp;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace asagiv.hullfinderUI
{
    public class MainWindowViewModel : ReactiveObject
    {
        #region Fields
        private bool _nextEnabled;
        private int _index;
        private GrahamHullFinder _grahamHullFinder;
        #endregion

        #region Properties
        public ObservableCollection<ISeries> Series { get; private set; }
        public bool NextEnabled
        {
            get { return _nextEnabled; }
            set { this.RaiseAndSetIfChanged(ref _nextEnabled, value); }
        }
        #endregion

        #region Commands
        public ICommand PlotDataCommand { get; }
        public ICommand NextStepCommand { get; }
        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            PlotDataCommand = ReactiveCommand.Create(PlotData);
            NextStepCommand = ReactiveCommand.Create(NextStep);

            Series = new ObservableCollection<ISeries>();
            _grahamHullFinder = new GrahamHullFinder();

            NextEnabled = false;
        }
        #endregion

        #region Methods
        public void PlotData()
        {
            Series.Clear();

            var values = new List<XyPoint>()
            {
                new(-1, -1),
                new(-1, 1),
                new(1, 1),
                new(1, -1),
                new(0, 0),
                new(3, 0),
                new(0, 2),
            };

            var points = values.Select(x => new ObservablePoint(x.X, x.Y));

            var pointSeries = new ScatterSeries<ObservablePoint>
            {
                Values = new ObservableCollection<ObservablePoint>(points),
                Stroke = new SolidColorPaint(SKColors.DodgerBlue),
                Fill = new SolidColorPaint(SKColors.White),
            };

            Series.Add(pointSeries);

            _grahamHullFinder = new GrahamHullFinder();

            _grahamHullFinder.GetPoints(values);

            var pointVectors = _grahamHullFinder.GetPointVectors();

            foreach (var vector in pointVectors)
            {
                var hullSeries = new LineSeries<ObservablePoint>
                {
                    Stroke = new SolidColorPaint(SKColors.Gray),
                    Fill = new SolidColorPaint(SKColors.Transparent),
                    LineSmoothness = 0,
                    Values = new ObservableCollection<ObservablePoint>
                    {
                        new(vector.SourcePoint.X, vector.SourcePoint.Y),
                        new(vector.TargetPoint.X, vector.TargetPoint.Y),
                    },
                };

                Series.Add(hullSeries);
            }

            NextEnabled = true;
            _index = 0;
        }

        public void NextStep()
        {
            var seriesToRemove = Series
                .FirstOrDefault(x => x.Name == "TraceLine");

            if(seriesToRemove != null)
            {
                Series.Remove(seriesToRemove);
            }

            _grahamHullFinder.NextPointVector();

            var hullPoints = _grahamHullFinder.HullPoints.Take(3).ToArray();

            var point1 = hullPoints[0];
            var point2 = hullPoints[1];
            var point3 = hullPoints[2];

            var series = new LineSeries<ObservablePoint>
            {
                Stroke = new SolidColorPaint(SKColors.Red),
                Fill = new SolidColorPaint(SKColors.Transparent),
                LineSmoothness = 0,
                Name = "TraceLine",
                Values = new ObservableCollection<ObservablePoint>
                {
                    new(point1.X, point1.Y),
                    new(point2.X, point2.Y),
                    new(point3.X, point3.Y),
                },
            };

            Series.Add(series);

            _index++;

            if(_grahamHullFinder.IsFinished)
            {
                NextEnabled = false;
            }
        }
        #endregion
    }
}
