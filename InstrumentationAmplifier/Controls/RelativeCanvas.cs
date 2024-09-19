using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace InstrumentationAmplifier.Controls;

/// <summary>
/// A panel that displays child controls at arbitrary locations.
/// </summary>
/// <remarks>
/// Unlike other <see cref="Panel"/> implementations, the <see cref="Canvas"/> doesn't lay out
/// its children in any particular layout. Instead, the positioning of each child control is
/// defined by the <code>Canvas.X</code>, <code>Canvas.Y</code>, <code>Canvas.Right</code>
/// and <code>Canvas.Bottom</code> attached properties.
/// </remarks>
public class RelativeCanvas : Panel, INavigableContainer
{
    /// <summary>
    /// Defines the Position attached property.
    /// </summary>
    public static readonly AttachedProperty<RelativeCanvasPosition> PositionProperty =
        AvaloniaProperty.RegisterAttached<Canvas, Control, RelativeCanvasPosition>(
            "Position", new RelativeCanvasPosition());


    /// <summary>
    /// Initializes static members of the <see cref="Canvas"/> class.
    /// </summary>
    static RelativeCanvas()
    {
        ClipToBoundsProperty.OverrideDefaultValue<Canvas>(false);
        AffectsParentArrange<Canvas>(PositionProperty);
    }
    
    /// <summary>
    /// Gets the value of the Position attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <returns>The control's position.</returns>
    public static RelativeCanvasPosition GetPosition(AvaloniaObject element)
    {
        return element.GetValue(PositionProperty);
    }

    /// <summary>
    /// Sets the value of the Position attached property for a control.
    /// </summary>
    /// <param name="element">The control.</param>
    /// <param name="value">The position value.</param>
    public static void SetPosition(AvaloniaObject element, RelativeCanvasPosition value)
    {
        element.SetValue(PositionProperty, value);
    }

    /// <summary>
    /// Gets the next control in the specified direction.
    /// </summary>
    /// <param name="direction">The movement direction.</param>
    /// <param name="from">The control from which movement begins.</param>
    /// <param name="wrap">Whether to wrap around when the first or last item is reached.</param>
    /// <returns>The control.</returns>
    IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        // TODO: Implement this
        return null;
    }

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        foreach (Control child in Children)
        {
            child.Measure(availableSize);
        }

        return new Size();
    }

    /// <summary>
    /// Arranges a single child.
    /// </summary>
    /// <param name="child">The child to arrange.</param>
    /// <param name="finalSize">The size allocated to the canvas.</param>
    protected virtual void ArrangeChild(Control child, Size finalSize)
    {
        var position = GetPosition(child);

        double x = finalSize.Width * position.X;
        if (position.XCenter) x-= child.DesiredSize.Width / 2;
        
        double y = finalSize.Height * position.Y;
        if (position.YCenter) y-= child.DesiredSize.Height / 2;

        child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
    }

    /// <summary>
    /// Arranges the control's children.
    /// </summary>
    /// <param name="finalSize">The size allocated to the control.</param>
    /// <returns>The space taken.</returns>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (Control child in Children)
        {
            ArrangeChild(child, finalSize);
        }

        return finalSize;
    }
}

/// <summary>
/// An element position for <see cref="RelativeCanvas"/>s.
/// </summary>
public class RelativeCanvasPosition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeCanvasPosition"/> class.
    /// </summary>
    public RelativeCanvasPosition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RelativeCanvasPosition"/> class.
    /// </summary>
    /// <param name="s">A string representation of the position.</param>
    public RelativeCanvasPosition(string s) : this()
    {
        bool TryParsePosition(ReadOnlySpan<char> part, out double pos, out bool center)
        {
            pos = 0;
            center = false;
            if (part.IsEmpty) return false;
            if (part[0] == '~') center = true;
            return double.TryParse(center ? part[1..] : part, CultureInfo.InvariantCulture,  out pos);
        }
        
        var ss = s.AsSpan().Trim();
        int spaceIndex = ss.IndexOf(' ');
        if (spaceIndex == -1)
        {
            if (TryParsePosition(ss, out double pos, out bool center))
            {
                X = Y = pos;
                XCenter = YCenter = center;
            }
            else
            {
                throw new FormatException(nameof(s));
            }
        }
        else
        {
            var sx = ss[..spaceIndex];
            var sy = ss[(spaceIndex + 1)..];
            
            if (TryParsePosition(sx, out double xPos, out bool xCenter) &&
                TryParsePosition(sy, out double yPos, out bool yCenter))
            {
                X = xPos;
                Y = yPos;
                XCenter = xCenter;
                YCenter = yCenter;
            }
            else
            {
                throw new FormatException(nameof(s));
            }
        }
    }

    public double X { get; set; } = 0;
    public double Y { get; set; } = 0;
    public bool XCenter { get; set; } = false;
    public bool YCenter { get; set; } = false;

    public override string ToString() => (XCenter ? "~" : "") + X + " " + (YCenter ? "~" : "") + Y;

    /// <summary>
    /// Parses a string representation of RelativeCanvas position.
    /// </summary>
    /// <param name="s">The position string.</param>
    /// <returns>The <see cref="RelativeCanvasPosition"/>.</returns>
    public static RelativeCanvasPosition Parse(string s) => new(s);
}