using System.Text;

namespace Rystem.Web.Components.Customization
{
    public class BreakPointWithFluidClassBuilder<T> : BreakPointClassBuilder<T>
       where T : ICssClassBuilder
    {
        internal BreakPointWithFluidClassBuilder(StringBuilder stringBuilder) : base(stringBuilder)
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
