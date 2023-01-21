using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace RepositoryFramework.Web.Components.Standard
{
    public partial class QueryFilter
    {
        [Parameter]
        public PropertyUiSettings? PropertyUiSettings { get; set; }
        [Parameter]
        public required ISearchValue SearchValue { get; set; }
        [Parameter]
        public required Action Search { get; set; }
        private IEnumerable<string>? _optionKeys { get; set; }
        private string _booleanSelectedKey = "None";
        private void Contains(ChangeEventArgs args)
        {
            var value = args?.Value?.ToString();
            if (value == null || value == string.Empty)
                SearchValue.UpdateLambda(null);
            else
                SearchValue.UpdateLambda($"x => x.{SearchValue.BaseProperty.GetFurtherProperty().Title}.Contains(\"{value}\")");
            Search();
        }
        private void SearchGreaterLesserThings(string? greaterThan, string? lesserThan)
        {
            if (greaterThan != null && lesserThan != null)
                SearchValue.UpdateLambda($"x => x.{SearchValue.BaseProperty.GetFurtherProperty().Title} >= {greaterThan} AndAlso x.{SearchValue.BaseProperty.GetFurtherProperty().Title} <= {lesserThan}");
            else if (greaterThan != null)
                SearchValue.UpdateLambda($"x => x.{SearchValue.BaseProperty.GetFurtherProperty().Title} >= {greaterThan}");
            else if (lesserThan != null)
                SearchValue.UpdateLambda($"x => x.{SearchValue.BaseProperty.GetFurtherProperty().Title} <= {lesserThan}");
            else
                SearchValue.UpdateLambda(null);
            Search();
        }
        private readonly ValueBearer<DateTime?> _dateTime = new();
        public void DateTimeSearch(ChangeEventArgs? args, bool atStart)
        {
            var value = args?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                if (atStart)
                    _dateTime.Start = default;
                else
                    _dateTime.End = default;
            }
            else
            {
                var date = DateTime.Parse(value);
                if (atStart)
                    _dateTime.Start = date;
                else
                    _dateTime.End = date;
            }
            SearchGreaterLesserThings(_dateTime.Start?.ToString(), _dateTime.End?.ToString());
        }

        private readonly ValueBearer<DateOnly?> _dateonly = new();
        public void DateSearch(ChangeEventArgs? args, bool atStart)
        {
            var value = args?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                if (atStart)
                    _dateonly.Start = default;
                else
                    _dateonly.End = default;
            }
            else
            {
                var date = DateOnly.Parse(value);
                if (atStart)
                    _dateonly.Start = date;
                else
                    _dateonly.End = date;
            }
            SearchGreaterLesserThings(_dateonly.Start?.ToString(), _dateonly.End?.ToString());
        }
        private readonly ValueBearer<decimal?> _number = new();
        public void NumberSearch(ChangeEventArgs args, bool atStart)
        {
            var value = args?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                if (atStart)
                    _number.Start = default;
                else
                    _number.End = default;
            }
            else
            {
                var parsedNumber = decimal.Parse(args.Value.ToString()!);
                if (atStart)
                    _number.Start = parsedNumber;
                else
                    _number.End = parsedNumber;
            }
            SearchGreaterLesserThings(_number.Start?.ToString(), _number.End?.ToString());
        }
        public void BoolSearch(LabelValueDropdownItem item, bool emptyIsValid)
        {
            var value = item.Value;
            if (value == null && emptyIsValid)
                SearchValue.UpdateLambda($"x => x.{SearchValue.BaseProperty.GetFurtherProperty().Title} == null");
            else if (value is bool booleanValue)
                SearchValue.UpdateLambda($"x => x.{SearchValue.BaseProperty.GetFurtherProperty().Title} == {booleanValue}");
            else
                SearchValue.UpdateLambda(null);
            _booleanSelectedKey = item.Id;
            Search();
        }

        public void MultipleChoices(IEnumerable<LabelValueDropdownItem> items)
        {
            if (items.Any())
            {
                StringBuilder builder = new();
                foreach (var id in items.Select(x => x.Value))
                {
                    if (builder.Length == 0)
                        builder.Append("x => ");
                    else
                        builder.Append(" OrElse ");
                    var value = PropertyUiSettings!.Values!.FirstOrDefault(x => x.Id == id)?.Value;
                    if (value.GetType().IsNumeric())
                        builder.Append($"x.{SearchValue.BaseProperty.GetFurtherProperty().Title} == {value}");
                    else
                        builder.Append($"x.{SearchValue.BaseProperty.GetFurtherProperty().Title} == \"{value}\"");
                }
                SearchValue.UpdateLambda(builder.ToString());
            }
            else
                SearchValue.UpdateLambda(null);
            Search();
        }
    }
}
