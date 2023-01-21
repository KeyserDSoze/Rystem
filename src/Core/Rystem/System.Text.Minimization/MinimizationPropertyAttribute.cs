namespace System.Text.Minimization
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MinimizationPropertyAttribute : Attribute
    {
        public int Column { get; }
        public MinimizationPropertyAttribute(int column)
            => Column = column;
    }
}
