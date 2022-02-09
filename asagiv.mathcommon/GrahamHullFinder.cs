namespace asagiv.mathcommon;

public class GrahamHullFinder
{
    #region Fields
    private Stack<XyPoint> _hullPointStack;
    #endregion

    #region Properties
    public IList<XyPoint> Points { get; private set; }
    public XyPoint FirstHullPoint { get; private set; }
    public IList<XyPoint> HullPoints => _hullPointStack.ToList();
    public IList<XyPointVector> AllPointVectors { get; private set; }
    public Queue<XyPoint> RemainingPoints { get; private set; }
    public bool IsFinished { get; private set; }
    #endregion

    #region Constructor
    public GrahamHullFinder()
    {
        Points = new List<XyPoint>();
        _hullPointStack = new Stack<XyPoint>();
    }
    #endregion

    public void GetPoints(IEnumerable<XyPoint> pointsInput)
    {
        IsFinished = false;

        // Order points. 
        Points = pointsInput
            .OrderBy(x => x.Y) // Lowest Y value first
            .ThenBy(x => x.X) // Lowest X value first
            .ToList();

        var convexHullPoints = new List<XyPoint>();

        if (pointsInput.Count() < 3)
        {
            throw new ArgumentException("Graham Hull Finder requires 3 or more points.");
        }

        FirstHullPoint = Points[0];

        _hullPointStack.Push(FirstHullPoint);

        convexHullPoints.Add(FirstHullPoint);
    }

    public IList<XyPointVector> GetPointVectors()
    {
        AllPointVectors = Points
            .Select(x => new XyPointVector(FirstHullPoint, x))
            .GroupBy(x => x.ConnectingVector.AngleRadians)
            .Select(x => x.MaxBy(y => y.ConnectingVector.Distance)) // Eliminate co-linear angles less than maximum distance.
            .Where(x => x is not null)
            .OrderBy(x => x.ConnectingVector.AngleRadians)
            .ToList();

        RemainingPoints = new Queue<XyPoint>(AllPointVectors.Select(x => x.TargetPoint));

        var secondHullPoint = RemainingPoints.Dequeue();

        _hullPointStack.Push(secondHullPoint);

        return AllPointVectors;
    }

    public void NextPointVector()
    {
        if(RemainingPoints.TryDequeue(out XyPoint nextPoint))
        {
            _hullPointStack.Push(nextPoint);
        }
        else
        {
            _hullPointStack.Push(FirstHullPoint);

            IsFinished = true;
        }
    }
}

public class XyPointVector
{
    public XyPoint SourcePoint { get; }
    public XyPoint TargetPoint { get; }
    public XyVector ConnectingVector { get; }

    public XyPointVector(XyPoint sourcePoint, XyPoint targetPoint)
    {
        SourcePoint = sourcePoint;
        TargetPoint = targetPoint;
        ConnectingVector = new XyVector(sourcePoint, targetPoint);
    }

    public override string ToString()
    {
        return ConnectingVector.ToString();
    }
}