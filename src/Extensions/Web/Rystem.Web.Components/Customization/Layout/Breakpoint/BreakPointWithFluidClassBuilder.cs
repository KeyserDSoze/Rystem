using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class BreakPointWithFluidClassBuilder<T> : BreakPointClassBuilder<T>
       where T : ICssClassBuilder
    {
        internal BreakPointWithFluidClassBuilder(StringBuilder stringBuilder, string prefix, bool prefixIsTurnedOff) : base(stringBuilder, prefix, prefixIsTurnedOff)
        {
        }
        public T Fluid
        {
            get
            {
                StringBuilder.Append($"{GetPrefix()}-fluid");
                return CreateNew<T>();
            }
        }
    }
}
