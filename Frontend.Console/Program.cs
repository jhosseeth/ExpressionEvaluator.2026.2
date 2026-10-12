using System.Globalization;
using Backend;

var examples = new (string Expression, double Expected)[]
{
	("4*5/(4+6)", 2),
	("4*(5+6-(8/2^3)-7)-1", 11),
	("4*7^(1/3)*7*((1+9)/3*7^4)", 428675.125184741),
	("144^(1/2)", 12),
	("12*3", 36),
	(".5+5.", 5.5),
	("-2^2 + (-2)^2 + 2^-2", 0.25),
	("2^3^2", 512),
};

foreach (var (expression, expected) in examples)
{
	Console.WriteLine($"Infix = {expression}, Result = {ExpressionEvaluator.Evaluate(expression):N5}");
	AssertResult(expression, expected);
}

RunAcceptanceChecks();

static void RunAcceptanceChecks()
{
	var validCases = new (string Expression, double Expected)[]
	{
		("12345", 12345),
		("12.5+0.75", 13.25),
		(".5+5.", 5.5),
		("1.25*2.4", 3),
		(" ( 12.5 + 0.75 ) * 2 ", 26.5),
		("2+3*4", 14),
		("(2+3)*4", 20),
		("((2+3)*(4-1))", 15),
		("8/4/2", 1),
		("2^3^2", 512),
		("-2^2", -4),
		("(-2)^2", 4),
		("2^-2", 0.25),
		("3*-2", -6),
		("2--3", 5),
		("-(2+3)", -5),
		("2*-(2+3)", -10),
		("1.5--.5", 2),
	};

	foreach (var (expression, expected) in validCases)
	{
		AssertResult(expression, expected);
	}

	var invalidCases = new[]
	{
		"",
		"   ",
		".",
		"1..2",
		"1.2.3",
		"2 3",
		"2(3)",
		"()",
		"(1+2",
		"1+2)",
		"1+",
		"*2",
		"1*/2",
		"2^^3",
		"-(2+)",
		"1e3",
	};

	foreach (var expression in invalidCases)
	{
		AssertThrows<FormatException>(expression);
	}

	AssertThrows<DivideByZeroException>("1/0");
	AssertThrows<DivideByZeroException>("1/-0");
	AssertThrows<ArithmeticException>("0^-1");
	AssertThrows<ArithmeticException>("(-1)^0.5");

	var originalCulture = CultureInfo.CurrentCulture;
	try
	{
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
		AssertResult("1.5+1.5", 3);
	}
	finally
	{
		CultureInfo.CurrentCulture = originalCulture;
	}

	// A failed expression must not leave values in a stack reused by later calls.
	AssertThrows<FormatException>("1+");
	AssertResult("7*8", 56);

	Console.WriteLine($"Acceptance checks passed: {validCases.Length} additional valid cases, {invalidCases.Length + 4} error cases, and invariant-culture check.");
}

static void AssertResult(string expression, double expected)
{
	var actual = ExpressionEvaluator.Evaluate(expression);
	var tolerance = 1e-10 * Math.Max(1, Math.Abs(expected));
	if (!double.IsFinite(actual) || Math.Abs(actual - expected) > tolerance)
	{
		throw new InvalidOperationException($"'{expression}' produced {actual}; expected {expected}.");
	}
}

static void AssertThrows<TException>(string expression) where TException : Exception
{
	try
	{
		ExpressionEvaluator.Evaluate(expression);
	}
	catch (TException)
	{
		return;
	}
	catch (Exception exception)
	{
		throw new InvalidOperationException($"'{expression}' threw {exception.GetType().Name}; expected {typeof(TException).Name}.", exception);
	}

	throw new InvalidOperationException($"'{expression}' did not throw {typeof(TException).Name}.");
}
