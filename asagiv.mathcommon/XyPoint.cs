using System.Diagnostics.CodeAnalysis;

namespace asagiv.mathcommon;

public struct XyPoint : IEquatable<XyPoint>
{
    #region Properties
    public double X { get; }
    public double Y { get; }
    #endregion

    #region Constructor
    public XyPoint(double X, double Y)
    {
        this.X = X;
        this.Y = Y;
    }
    #endregion

    #region Methods
    public bool Equals(XyPoint target)
    {
        return X == target.X && Y == target.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is XyPoint && Equals((XyPoint)obj);
    }

    public override string ToString()
    {
        return $"X = {X}, Y = {Y}";
    }

    public static bool operator ==(XyPoint left, XyPoint right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(XyPoint left, XyPoint right)
    {
        return !(left == right);
    }
    #endregion
}
