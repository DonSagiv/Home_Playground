namespace asagiv.mathcommon;

public class XyVector : IComparable<XyVector>
{
    #region Properties
    public double DeltaX { get; }
    public double DeltaY { get; }
    public double Slope { get; }
    public double Distance { get; }
    public double AngleRadians { get; }
    public double AngleDegrees => AngleRadians * 180 / Math.PI;
    #endregion

    #region Constructor
    public XyVector(XyPoint source, XyPoint target)
    {
        DeltaX = target.X - source.X;
        DeltaY = target.Y - source.Y;
        Slope = DeltaY / DeltaX;
        Distance = Math.Sqrt(DeltaX * DeltaX + DeltaY * DeltaY);
        AngleRadians = Math.Atan2(DeltaY, DeltaX);

        // Ensure no negative angles. Should be from 0 to 180.
        if(AngleRadians < 0)
        {
            AngleRadians += 2 * Math.PI;
        }
    }
    #endregion

    #region Methods
    public int CompareTo(XyVector target)
    {
        if(target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var orientationValue = DeltaX * target.DeltaY - DeltaY * target.DeltaX;

        if(orientationValue > 0)
        {
            return 1; // Counter-clockwise, left-turning.
        }
        else if(orientationValue <= 0)
        {
            return -1; // Clockwise, right-turning
        }

        return 0; // Co-linear, no turning.
    }

    public override string ToString()
    {
        return $"Slope = {Slope}, Angle = {AngleDegrees}°, Distance = {Distance}";
    }
    #endregion
}