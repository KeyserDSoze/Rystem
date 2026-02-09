using System;
using System.Collections.Generic;
using System.Linq;
using RepositoryFramework.Tools.TypescriptGenerator.Analysis;
using RepositoryFramework.Tools.TypescriptGenerator.Domain;
using RepositoryFramework.Tools.TypescriptGenerator.Generation.TypeScript;
using Xunit;
using static RepositoryFramework.UnitTest.TypescriptGenerator.TestDescriptorFactory;

namespace RepositoryFramework.UnitTest.TypescriptGenerator;

public class DateConversionTest
{
    // ============================
    // TypeDescriptor tests
    // ============================

    [Fact]
    public void TypeDescriptor_DateTime_IsDateTrue()
    {
        var type = DateTimeType;
        Assert.True(type.IsDate);
        Assert.Equal(DateTypeKind.DateTime, type.DateKind);
        Assert.True(type.ContainsDateType);
    }

    [Fact]
    public void TypeDescriptor_DateTimeOffset_IsDateTrue()
    {
        var type = DateTimeOffsetType;
        Assert.True(type.IsDate);
        Assert.Equal(DateTypeKind.DateTimeOffset, type.DateKind);
    }

    [Fact]
    public void TypeDescriptor_DateOnly_IsDateTrue()
    {
        var type = DateOnlyType;
        Assert.True(type.IsDate);
        Assert.Equal(DateTypeKind.DateOnly, type.DateKind);
    }

    [Fact]
    public void TypeDescriptor_String_IsDateFalse()
    {
        Assert.False(StringType.IsDate);
        Assert.Null(StringType.DateKind);
        Assert.False(StringType.ContainsDateType);
    }

    [Fact]
    public void TypeDescriptor_ArrayOfDates_ContainsDateType()
    {
        var arrayType = CreateArrayType(DateTimeType);
        Assert.False(arrayType.IsDate);
        Assert.True(arrayType.ContainsDateType);
    }

    [Fact]
    public void TypeDescriptor_DictWithDateValues_ContainsDateType()
    {
        var dictType = CreateDictionaryType(StringType, DateOnlyType);
        Assert.False(dictType.IsDate);
        Assert.True(dictType.ContainsDateType);
    }

    [Fact]
    public void TypeDescriptor_ArrayOfStrings_ContainsDateTypeFalse()
    {
        var arrayType = CreateArrayType(StringType);
        Assert.False(arrayType.ContainsDateType);
    }

    // ============================
    // ModelDescriptor.RequiresRawType
    // ============================

    [Fact]
    public void ModelDescriptor_WithDateProperty_RequiresRawType()
    {
        var model = CreateModel("Event",
            CreateProperty("Name", StringType),
            CreateProperty("StartDate", DateTimeType));

        Assert.True(model.RequiresRawType);
    }

    [Fact]
    public void ModelDescriptor_WithoutDateOrCustomJson_DoesNotRequireRawType()
    {
        var model = CreateModel("Simple",
            CreateProperty("Name", StringType),
            CreateProperty("Count", NumberType));

        Assert.False(model.RequiresRawType);
    }

    [Fact]
    public void ModelDescriptor_WithDateArrayProperty_RequiresRawType()
    {
        var model = CreateModel("Schedule",
            CreateProperty("Name", StringType),
            CreateProperty("Dates", CreateArrayType(DateOnlyType)));

        Assert.True(model.RequiresRawType);
    }

    // ============================
    // CleanTypeEmitter
    // ============================

    [Fact]
    public void CleanType_DateTimeProperty_EmitsDateType()
    {
        var model = CreateModel("Event",
            CreateProperty("Name", StringType),
            CreateProperty("CreatedAt", DateTimeType));

        var context = new EmitterContext();
        var result = CleanTypeEmitter.Emit(model, context);

        Assert.Contains("createdAt: Date;", result);
        Assert.Contains("name: string;", result);
    }

    [Fact]
    public void CleanType_DateOnlyProperty_EmitsDateType()
    {
        var model = CreateModel("Person",
            CreateProperty("BirthDate", DateOnlyType));

        var context = new EmitterContext();
        var result = CleanTypeEmitter.Emit(model, context);

        Assert.Contains("birthDate: Date;", result);
    }

    [Fact]
    public void CleanType_DateTimeOffsetProperty_EmitsDateType()
    {
        var model = CreateModel("Audit",
            CreateProperty("Timestamp", DateTimeOffsetType));

        var context = new EmitterContext();
        var result = CleanTypeEmitter.Emit(model, context);

        Assert.Contains("timestamp: Date;", result);
    }

    [Fact]
    public void CleanType_OptionalDateProperty_EmitsOptionalDate()
    {
        var model = CreateModel("Event",
            CreateProperty("DeletedAt", DateTimeType, isOptional: true));

        var context = new EmitterContext();
        var result = CleanTypeEmitter.Emit(model, context);

        Assert.Contains("deletedAt?: Date;", result);
    }

    [Fact]
    public void CleanType_ArrayOfDates_EmitsDateArray()
    {
        var model = CreateModel("Schedule",
            CreateProperty("Dates", CreateArrayType(DateOnlyType)));

        var context = new EmitterContext();
        var result = CleanTypeEmitter.Emit(model, context);

        Assert.Contains("dates: Date[];", result);
    }

    [Fact]
    public void CleanType_DictOfDates_EmitsRecordWithDate()
    {
        var model = CreateModel("Log",
            CreateProperty("Entries", CreateDictionaryType(StringType, DateTimeType)));

        var context = new EmitterContext();
        var result = CleanTypeEmitter.Emit(model, context);

        Assert.Contains("entries: Record<string, Date>;", result);
    }

    // ============================
    // RawTypeEmitter
    // ============================

    [Fact]
    public void RawType_DateTimeProperty_EmitsStringType()
    {
        var model = CreateModel("Event",
            CreateProperty("CreatedAt", DateTimeType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        var result = RawTypeEmitter.Emit(model, context);

        Assert.Contains("CreatedAt: string;", result);
        Assert.DoesNotContain("Date", result);
    }

    [Fact]
    public void RawType_ArrayOfDates_EmitsStringArray()
    {
        var model = CreateModel("Schedule",
            CreateProperty("Dates", CreateArrayType(DateOnlyType)));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Schedule");
        var result = RawTypeEmitter.Emit(model, context);

        Assert.Contains("Dates: string[];", result);
    }

    // ============================
    // MapperEmitter - Raw -> Clean
    // ============================

    [Fact]
    public void Mapper_DateTime_UsesParseDateTime()
    {
        var model = CreateModel("Event",
            CreateProperty("CreatedAt", DateTimeType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("parseDateTime(raw.CreatedAt)", result);
    }

    [Fact]
    public void Mapper_DateTimeOffset_UsesParseDateTimeOffset()
    {
        var model = CreateModel("Audit",
            CreateProperty("Timestamp", DateTimeOffsetType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Audit");
        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("parseDateTimeOffset(raw.Timestamp)", result);
    }

    [Fact]
    public void Mapper_DateOnly_UsesParseDateOnly()
    {
        var model = CreateModel("Person",
            CreateProperty("BirthDate", DateOnlyType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Person");
        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("parseDateOnly(raw.BirthDate)", result);
    }

    [Fact]
    public void Mapper_OptionalDateTime_UsesConditionalParse()
    {
        var model = CreateModel("Event",
            CreateProperty("DeletedAt", DateTimeType, isOptional: true));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("raw.DeletedAt != null ? parseDateTime(raw.DeletedAt) : undefined", result);
    }

    [Fact]
    public void Mapper_ArrayOfDates_UsesParseDateMap()
    {
        var model = CreateModel("Schedule",
            CreateProperty("Dates", CreateArrayType(DateOnlyType)));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Schedule");
        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("raw.Dates?.map(parseDateOnly) ?? []", result);
    }

    [Fact]
    public void Mapper_DictWithDateValues_UsesParseDateEntries()
    {
        var model = CreateModel("Log",
            CreateProperty("Entries", CreateDictionaryType(StringType, DateTimeType)));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Log");
        var result = MapperEmitter.EmitRawToClean(model, context);

        Assert.Contains("parseDateTime(v)", result);
        Assert.Contains("Object.fromEntries", result);
    }

    // ============================
    // MapperEmitter - Clean -> Raw
    // ============================

    [Fact]
    public void Mapper_CleanToRaw_DateTime_UsesFormatDateTime()
    {
        var model = CreateModel("Event",
            CreateProperty("CreatedAt", DateTimeType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("formatDateTime(clean.createdAt)", result);
    }

    [Fact]
    public void Mapper_CleanToRaw_DateOnly_UsesFormatDateOnly()
    {
        var model = CreateModel("Person",
            CreateProperty("BirthDate", DateOnlyType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Person");
        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("formatDateOnly(clean.birthDate)", result);
    }

    [Fact]
    public void Mapper_CleanToRaw_OptionalDate_UsesConditionalFormat()
    {
        var model = CreateModel("Event",
            CreateProperty("DeletedAt", DateTimeType, isOptional: true));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("clean.deletedAt != null ? formatDateTime(clean.deletedAt) : undefined", result);
    }

    [Fact]
    public void Mapper_CleanToRaw_ArrayOfDates_UsesFormatDateMap()
    {
        var model = CreateModel("Schedule",
            CreateProperty("Dates", CreateArrayType(DateOnlyType)));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Schedule");
        var result = MapperEmitter.EmitCleanToRaw(model, context);

        Assert.Contains("clean.dates?.map(formatDateOnly) ?? []", result);
    }

    // ============================
    // DateMapperEmitter
    // ============================

    [Fact]
    public void DateMapperEmitter_EmitsAllFunctions()
    {
        var result = DateMapperEmitter.Emit();

        Assert.Contains("export function parseDateTime(value: string): Date", result);
        Assert.Contains("export function formatDateTime(date: Date): string", result);
        Assert.Contains("export function parseDateTimeOffset(value: string): Date", result);
        Assert.Contains("export function formatDateTimeOffset(date: Date): string", result);
        Assert.Contains("export function parseDateOnly(value: string): Date", result);
        Assert.Contains("export function formatDateOnly(date: Date): string", result);
    }

    [Fact]
    public void DateMapperEmitter_ParseDateTimeUsesNewDate()
    {
        var result = DateMapperEmitter.Emit();
        Assert.Contains("return new Date(value);", result);
    }

    [Fact]
    public void DateMapperEmitter_ParseDateOnlyAppendsMidnight()
    {
        var result = DateMapperEmitter.Emit();
        Assert.Contains("return new Date(value + 'T00:00:00');", result);
    }

    [Fact]
    public void DateMapperEmitter_FormatDateOnlyUsesLocalDateParts()
    {
        var result = DateMapperEmitter.Emit();
        Assert.Contains("date.getFullYear()", result);
        Assert.Contains("date.getMonth()", result);
        Assert.Contains("date.getDate()", result);
    }

    // ============================
    // ImportResolver - Date imports
    // ============================

    [Fact]
    public void ImportResolver_DateProperty_ImportsParseFunctions()
    {
        var model = CreateModel("Event",
            CreateProperty("CreatedAt", DateTimeType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        context.AllModels["Event"] = model;

        var imports = ImportResolver.ResolveImports("Event", [model], context, null);

        Assert.Contains("parseDateTime", imports);
        Assert.Contains("formatDateTime", imports);
        Assert.Contains("DateMappers", imports);
    }

    [Fact]
    public void ImportResolver_DateOnlyProperty_ImportsParseDateOnly()
    {
        var model = CreateModel("Person",
            CreateProperty("BirthDate", DateOnlyType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Person");
        context.AllModels["Person"] = model;

        var imports = ImportResolver.ResolveImports("Person", [model], context, null);

        Assert.Contains("parseDateOnly", imports);
        Assert.Contains("formatDateOnly", imports);
    }

    [Fact]
    public void ImportResolver_NoDateProperty_NoParseFunctionImports()
    {
        var model = CreateModel("Simple",
            CreateProperty("Name", StringType));

        var context = new EmitterContext();
        context.AllModels["Simple"] = model;

        var imports = ImportResolver.ResolveImports("Simple", [model], context, null);

        Assert.DoesNotContain("parseDateTime", imports);
        Assert.DoesNotContain("DateMappers", imports);
    }

    [Fact]
    public void ImportResolver_DateImportsUseFunctionSyntax()
    {
        var model = CreateModel("Event",
            CreateProperty("CreatedAt", DateTimeType));

        var context = new EmitterContext();
        context.TypesRequiringRaw.Add("Event");
        context.AllModels["Event"] = model;

        var imports = ImportResolver.ResolveImports("Event", [model], context, null);

        // Date functions use "import { }" not "import type { }"
        Assert.Contains("import {", imports);
        Assert.DoesNotContain("import type { formatDateTime", imports);
    }

    // ============================
    // End-to-end: TypeResolver with real types
    // ============================

    [Fact]
    public void TypeResolver_DateTime_SetsDateKind()
    {
        var resolver = new TypeResolver();
        var descriptor = resolver.Resolve(typeof(DateTime));

        Assert.True(descriptor.IsPrimitive);
        Assert.True(descriptor.IsDate);
        Assert.Equal(DateTypeKind.DateTime, descriptor.DateKind);
        Assert.Equal("string", descriptor.TypeScriptName);
    }

    [Fact]
    public void TypeResolver_DateTimeOffset_SetsDateKind()
    {
        var resolver = new TypeResolver();
        var descriptor = resolver.Resolve(typeof(DateTimeOffset));

        Assert.True(descriptor.IsDate);
        Assert.Equal(DateTypeKind.DateTimeOffset, descriptor.DateKind);
    }

    [Fact]
    public void TypeResolver_DateOnly_SetsDateKind()
    {
        var resolver = new TypeResolver();
        var descriptor = resolver.Resolve(typeof(DateOnly));

        Assert.True(descriptor.IsDate);
        Assert.Equal(DateTypeKind.DateOnly, descriptor.DateKind);
    }

    [Fact]
    public void TypeResolver_NullableDateTime_SetsDateKind()
    {
        var resolver = new TypeResolver();
        var descriptor = resolver.Resolve(typeof(DateTime?));

        Assert.True(descriptor.IsDate);
        Assert.True(descriptor.IsNullable);
        Assert.Equal(DateTypeKind.DateTime, descriptor.DateKind);
    }

    [Fact]
    public void TypeResolver_TimeOnly_NotADateType()
    {
        var resolver = new TypeResolver();
        var descriptor = resolver.Resolve(typeof(TimeOnly));

        Assert.True(descriptor.IsPrimitive);
        Assert.False(descriptor.IsDate);
        Assert.Null(descriptor.DateKind);
        Assert.Equal("string", descriptor.TypeScriptName);
    }

    [Fact]
    public void TypeResolver_TimeSpan_NotADateType()
    {
        var resolver = new TypeResolver();
        var descriptor = resolver.Resolve(typeof(TimeSpan));

        Assert.True(descriptor.IsPrimitive);
        Assert.False(descriptor.IsDate);
        Assert.Equal("string", descriptor.TypeScriptName);
    }

    // ============================
    // Full model analysis
    // ============================

    [Fact]
    public void ModelAnalyzer_ModelWithDates_RequiresRawType()
    {
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(EventWithDates));

        Assert.True(model.RequiresRawType);
        var createdAt = model.Properties.First(p => p.CSharpName == "CreatedAt");
        Assert.True(createdAt.Type.IsDate);
        Assert.Equal(DateTypeKind.DateTime, createdAt.Type.DateKind);
    }

    [Fact]
    public void ModelAnalyzer_ModelWithDateOnly_RequiresRawType()
    {
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(PersonWithBirthDate));

        Assert.True(model.RequiresRawType);
        var birthDate = model.Properties.First(p => p.CSharpName == "BirthDate");
        Assert.True(birthDate.Type.IsDate);
        Assert.Equal(DateTypeKind.DateOnly, birthDate.Type.DateKind);
    }

    [Fact]
    public void ModelAnalyzer_ModelWithNoDate_DoesNotRequireRawType()
    {
        var analyzer = new ModelAnalyzer();
        var model = analyzer.Analyze(typeof(SimpleNoDates));

        Assert.False(model.RequiresRawType);
    }
}

// Test models in the same file-scoped namespace
public class EventWithDates
{
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class PersonWithBirthDate
{
    public string FullName { get; set; } = "";
    public DateOnly BirthDate { get; set; }
}

public class SimpleNoDates
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class ScheduleWithDateArrays
{
    public string Title { get; set; } = "";
    public List<DateTime> EventDates { get; set; } = [];
    public Dictionary<string, DateOnly> Holidays { get; set; } = [];
}
