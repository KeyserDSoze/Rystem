using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class BreakPointWithFluidClassBuilder<T> : BreakPointClassBuilder<T>
       where T : ICssClassBuilder
    {
        internal BreakPointWithFluidClassBuilder(StringBuilder stringBuilder, string prefix) : base(stringBuilder, prefix)
        {
        }
        public T Fluid
        {
            get
            {
                StringBuilder.Append($"{Prefix}-fluid");
                return CreateNew<T>();
            }
        }
    }
}
