### [What is Rystem?](https://github.com/KeyserDSoze/RystemV3)

## Get Started

### Extension methods

#### Stopwatch
You can monitor the time spent on an action, task or in a method.
Some examples from Unit test.

	var started = Stopwatch.Start();
    //do something
    await Task.Delay(2000);
    var result = started.Stop();

or

    var result = await Stopwatch.MonitorAsync(async () =>
    {
        await Task.Delay(2000);
    });

or with a return value

     var result = await Stopwatch.MonitorAsync(async () =>
    {
        await Task.Delay(2000);
        return 3;
    });

### Linq expression serializer
Usually a linq expression is not serializable as string. With this method you can serialize your expression with some limits. Only primitives are allowed in the expression body.
An example from Unit test.

    Expression<Func<MakeIt, bool>> expression = ƒ => ƒ.X == q && ƒ.Samules.Any(x => x == k) && ƒ.Sol && (ƒ.X.Contains(q) || ƒ.Sol.Equals(IsOk)) && (ƒ.E == id | ƒ.Id == V) && (ƒ.Type == MakeType.Yes || ƒ.Type == qq);
    var serialized = expression.Serialize();

with result

    "ƒ => ((((((ƒ.X == \"dasda\") AndAlso ƒ.Samules.Any(x => (x == \"ccccde\"))) AndAlso ƒ.Sol) AndAlso (ƒ.X.Contains(\"dasda\") OrElse ƒ.Sol.Equals(True))) AndAlso ((ƒ.E == Guid.Parse(\"bf46510b-b7e6-4ba2-88da-cef208aa81f2\")) Or (ƒ.Id == 32))) AndAlso ((ƒ.Type == 1) OrElse (ƒ.Type == 2)))"

with deserialization

    var newExpression = expressionAsString.Deserialize<MakeIt, bool>();

and usage, for instance, with Linq

    var result = makes.Where(newExpression.Compile()).ToList();

you can deserialize and compile at the same time with

    var newExpression = expressionAsString.DeserializeAndCompile<MakeIt, bool>();

you can deserialize as dynamic and use the linq dynamic methods

    Expression<Func<MakeIt, int>> expression = x => x.Id;
    string value = expression.Serialize();
    LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();
    var got = makes.AsQueryable();
    var cut = got.OrderByDescending(newLambda).ThenByDescending(newLambda).ToList();

please see the unit test [here](https://github.com/KeyserDSoze/RystemV3/blob/master/src/Rystem.Test/Rystem.Test.UnitTest/System.Linq/SystemLinq.cs) to understand better how it works
You may deal with return type of your lambda expression:

     LambdaExpression newLambda = value.DeserializeAsDynamic<MakeIt>();
     newLambda =  newLambda.ChangeReturnType<bool>();

or

     newLambda =  newLambda.ChangeReturnType(typeof(bool));


### Reflection helper

#### Name of calling class
You can find the name of the calling class from your method, with deep = 1 the calling class of your method, with deep = 2 the calling class that calls the class that calls your method, and so on, with fullName set to true you obtain the complete name of the discovered class.

    ReflectionHelper.NameOfCallingClass(deep, fullName);

#### Extensions for Type class
You can get the properties, fields and constructors for your class (and singleton them to save time during new requests)

    Type.FetchProperties();
    Type.FecthConstructors();
    Type.FetchFields();

You can check if a Type is a son or a father or both of other type (in the example Zalo and Folli are Sulo).
You may find more information in unit test [here](https://github.com/KeyserDSoze/RystemV3/blob/master/src/Rystem.Test/Rystem.Test.UnitTest/System.Reflection/ReflectionTest.cs)
    
    Zalo zalo = new();
    Zalo zalo2 = new();
    Folli folli = new();
    Sulo sulo = new();
    object quo = new();
    int x = 2;
    decimal y = 3;
    Assert.True(zalo.IsTheSameTypeOrASon(sulo));
    Assert.True(folli.IsTheSameTypeOrASon(sulo));
    Assert.True(zalo.IsTheSameTypeOrASon(zalo2));
    Assert.True(zalo.IsTheSameTypeOrASon(quo));
    Assert.False(sulo.IsTheSameTypeOrASon(zalo));
    Assert.True(sulo.IsTheSameTypeOrAParent(zalo));
    Assert.False(y.IsTheSameTypeOrAParent(x));

#### Mock a Type
If you need to create a type over an abstract class or interface you may use the mocking system of Rystem.
For example, if you have an abstract class like this one down below.

    public abstract class Alzio
    {
        private protected string X { get; }
        public string O => X;
        public string A { get; set; }
        public Alzio(string x)
        {
            X = x;
        }
    }

you can create an instace of it or simply mock it with

    var mocked = typeof(Alzio).CreateInstance("AAA") as Alzio;
    mocked.A = "rrrr";

and you can use the class like a real class. You also may do it with

    Alzio alzio = null!;
    var mocked = alzio.CreateInstance("AAA");
    mocked.A = "rrrr";

or

    Mocking.CreateInstance<Alzio>("AAA");

you may see "AAA" as argument for your constructor in abstract class.

### Text extensions
You may convert as fast as possible byte[] to string or stream to byte[] or byte[] to stream or stream to string or string to stream.
For example, string to byte array and viceversa.

    string olfa = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";
    var bytes = olfa.ToByteArray();
    string value = bytes.ConvertToString();

For example, string to stream and viceversa.

    string olfa = "daskemnlandxioasndslam dasmdpoasmdnasndaslkdmlasmv asmdsa";
    var stream = olfa.ToStream();
    string value = stream.ConvertToString();

You may read a string with break lines as an enumerable of string

    string olfa = "daskemnlandxioasndslam\ndasmdpoasmdnasndaslkdmlasmv\nasmdsa";
    var stream = olfa.ToStream();
    var strings = new List<string>();
    await foreach (var x in stream.ReadLinesAsync())
    {
        strings.Add(x);
    }

A simple method to make uppercase the first character.

    string olfa = "dasda";
    var olfa2 = olfa.ToUpperCaseFirst();

A simple method to check if a char is contained at least X times.

    string value = "abcderfa";
    bool containsAtLeastTwoAChar = value.ContainsAtLeast(2, 'a');

### Character separated-value (CSV)
Transform any kind of IEnumerable data in a CSV string.

    string value = _models.ToCsv();


### Minimization of a model (based on CSV concept)
It's a brand new idea to serialize any kind of objects (with lesser occupied space of json), the idea comes from Command separated-value standard.
To serialize

    string value = _models.ToMinimize();

To deserialize (for instance in a List of a class named CsvModel)

    value.FromMinimization<List<CsvModel>>();

### Extensions for json
I don't know if you are fed up to write JsonSerializer.Serialize, I do, and so, you may use the extension method to serialize faster.
To serialize

    var text = value.ToJson();

To deserialize in a class (for instance a class named User)

    var value = text.FromJson<User>();

### Extensions for Task
I don't know if you still are fed up to write .ConfigureAwait(false) to eliminate the context waiting for a task. I do.
[Why should I set the configure await to false?](https://devblogs.microsoft.com/dotnet/configureawait-faq/)
To set configure await to false

    await {your async task}.NoContext();

Instead, to get the result as synchronous result but with a configure await set to false.

    {your async task}.ToResult();

You may change the behavior of your NoContext() or ToResult(), setting (in the bootstrap of your application for example)

    RystemTask.WaitYourStartingThread = true;

When do I need a true? In windows application for example you have to return after a button clicked to the same thread that started the request.

## Dependency injection extensions

### Warm up
When you use the DI pattern in your .Net application you could need a warm up after the build of your services. And with Rystem you can simply do it.

	builder.Services.AddWarmUp(() => somethingToDo());

and after the build use the warm up

	var app = builder.Build();
	await app.Services.WarmUpAsync();


## Population service
You can use the population service to create a list of random value of a specific Type.
An example from unit test explains how to use the service.

    IServiceCollection services = new ServiceCollection();
    services.AddPopulationService();
    var serviceProvider = services.BuildServiceProvider().CreateScope().ServiceProvider;
    var populatedModel = serviceProvider.GetService<IPopulation<PopulationModelTest>>();
    IPopulation<PopulationModelTest> allPrepopulation = populatedModel!
        .Setup()
        .WithPattern(x => x.J!.First().A, "[a-z]{4,5}")
            .WithPattern(x => x.Y!.First().Value.A, "[a-z]{4,5}")
            .WithImplementation(x => x.I, typeof(MyInnerInterfaceImplementation))
            .WithPattern(x => x.I!.A!, "[a-z]{4,5}")
            .WithPattern(x => x.II!.A!, "[a-z]{4,5}")
            .WithImplementation<IInnerInterface, MyInnerInterfaceImplementation>(x => x.I!);
    var all = allPrepopulation.Populate();