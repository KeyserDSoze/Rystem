using System.Reflection;

namespace RepositoryFramework.Web.Components
{
    public static class BasePropertyExtensions
    {
        public static FurtherProperty GetFurtherProperty(this BaseProperty baseProperty)
            => baseProperty.GetProperty<FurtherProperty>(Constant.FurtherProperty);
    }
}
