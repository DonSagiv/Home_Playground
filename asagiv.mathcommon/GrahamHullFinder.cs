namespace asagiv.mathcommon;

public class GrahamHullFinder
{
    #region Properties
    public IList<XyPoint> points { get; private set; }
    public XyPoint firstHullPoint { get; private set; }
    public IList<XyPoint> hullPoints { get; private set; }
    public IList<XyPointVector> pointVectors { get; private set; }
    #endregion

    #region Constructor
    public GrahamHullFinder()
    {
        points = new List<XyPoint>();
        hullPoints = new List<XyPoint>();
    }
    #endregion

    public void GetPoints(IEnumerable<XyPoint> pointsInput)
    {
        // Order points. 
        points = pointsInput
            .OrderBy(x => x.Y) // Lowest Y value first
            .ThenBy(x => x.X) // Lowest X value first
            .ToList();

        var convexHullPoints = new List<XyPoint>();

        if (pointsInput.Count() < 3)
        {
            throw new ArgumentException("Graham Hull Finder requires 3 or more points.");
        }

        firstHullPoint = points[0];

        convexHullPoints.Add(firstHullPoint);
    }

    public IList<XyPointVector> GetPointVectors()
    {
        pointVectors = points
            .Select(x => new XyPointVector(firstHullPoint, x))
            .GroupBy(x => x.ConnectingVector.AngleRadians)
            .Select(x => x.MaxBy(y => y.ConnectingVector.Distance)) // Eliminate co-linear angles less than maximum distance.
            .Where(x => x is not null)
            .OrderBy(x => x.ConnectingVector.AngleRadians)
            .ToList();

        return pointVectors;
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