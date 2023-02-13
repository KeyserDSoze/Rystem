namespace Rystem.Web.Components.Customization
{
    public class ClassBuilder
    {
        //public ContainerBuilder Container
        //{
        //    get
        //    {
        //        StringBuilder.Append(" container");
        //        return CreateNew<ContainerBuilder>();
        //    }
        //}
        //public ColumnBuilder Col
        //{
        //    get
        //    {
        //        StringBuilder.Append(" col");
        //        return CreateNew<ColumnBuilder>();
        //    }
        //}
        public RowBuilder Row
        {
            get
            {
                var style = RowBuilder.Style;
                RowBuilder.Style.StringBuilder.Append(" row");
                return style;
            }
        }
    }
}
