using System.Globalization;

namespace Backend;

public static class ExpressionEvaluator
{
    private enum TokenType
    {
        Number,
        Operator,
        LeftParenthesis,
        RightParenthesis,
    }

    private readonly record struct Token(TokenType Type, string Text, double Value = 0);

    public static double Evaluate(string infix)
    {
        ArgumentNullException.ThrowIfNull(infix);

        var tokens = Tokenize(infix);
        var postfix = ToPostfix(tokens);
        return EvaluatePostfix(postfix);
    }

    // Kept for compatibility with the original project API.
    public static double Evalute(string infix) => Evaluate(infix);

    private static List<Token> Tokenize(string infix)
    {
        var tokens = new List<Token>();

        for (var index = 0; index < infix.Length;)
        {
            var current = infix[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (IsAsciiDigit(current) || current == '.')
            {
                var start = index;
                var hasDecimalPoint = false;
                var hasDigit = false;

                while (index < infix.Length && (IsAsciiDigit(infix[index]) || infix[index] == '.'))
                {
                    if (infix[index] == '.')
                    {
                        if (hasDecimalPoint)
                        {
                            throw new FormatException("Un número no puede tener más de un punto decimal.");
                        }

                        hasDecimalPoint = true;
                    }
                    else
                    {
                        hasDigit = true;
                    }

                    index++;
                }

                var text = infix[start..index];
                if (!hasDigit || !double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
                {
                    throw new FormatException($"El número '{text}' no es válido o está fuera del rango permitido.");
                }

                tokens.Add(new Token(TokenType.Number, text, value));
                continue;
            }

            if (current is '+' or '-' or '*' or '/' or '^')
            {
                tokens.Add(new Token(TokenType.Operator, current.ToString()));
            }
            else if (current == '(')
            {
                tokens.Add(new Token(TokenType.LeftParenthesis, current.ToString()));
            }
            else if (current == ')')
            {
                tokens.Add(new Token(TokenType.RightParenthesis, current.ToString()));
            }
            else
            {
                throw new FormatException($"El carácter '{current}' no es válido en la expresión.");
            }

            index++;
        }

        if (tokens.Count == 0)
        {
            throw new FormatException("La expresión está vacía.");
        }

        return tokens;
    }

    private static List<Token> ToPostfix(List<Token> infix)
    {
        var postfix = new List<Token>();
        var operators = new Stack<Token>();
        var expectingOperand = true;

        foreach (var token in infix)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    if (!expectingOperand)
                    {
                        throw new FormatException("Falta un operador entre dos números.");
                    }

                    postfix.Add(token);
                    expectingOperand = false;
                    break;

                case TokenType.LeftParenthesis:
                    if (!expectingOperand)
                    {
                        throw new FormatException("Falta un operador antes del paréntesis de apertura.");
                    }

                    operators.Push(token);
                    expectingOperand = true;
                    break;

                case TokenType.RightParenthesis:
                    if (expectingOperand)
                    {
                        throw new FormatException("Falta un número antes del paréntesis de cierre.");
                    }

                    while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParenthesis)
                    {
                        postfix.Add(operators.Pop());
                    }

                    if (operators.Count == 0)
                    {
                        throw new FormatException("Hay un paréntesis de cierre sin su paréntesis de apertura.");
                    }

                    operators.Pop();
                    expectingOperand = false;
                    break;

                case TokenType.Operator:
                    var isUnaryMinus = token.Text == "-" && expectingOperand;
                    if (expectingOperand && !isUnaryMinus)
                    {
                        throw new FormatException($"El operador '{token.Text}' no tiene un operando a su izquierda.");
                    }

                    if (isUnaryMinus)
                    {
                        // Unary minus binds less tightly than exponentiation.
                        operators.Push(new Token(TokenType.Operator, "u-"));
                        break;
                    }

                    while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator && ShouldPop(operators.Peek(), token))
                    {
                        postfix.Add(operators.Pop());
                    }

                    operators.Push(token);
                    expectingOperand = true;
                    break;
            }
        }

        if (expectingOperand)
        {
            throw new FormatException("La expresión termina donde se esperaba un número.");
        }

        while (operators.Count > 0)
        {
            var op = operators.Pop();
            if (op.Type == TokenType.LeftParenthesis)
            {
                throw new FormatException("Hay un paréntesis de apertura sin su paréntesis de cierre.");
            }

            postfix.Add(op);
        }

        return postfix;
    }

    private static bool ShouldPop(Token stackedOperator, Token incomingOperator)
    {
        var stackedPrecedence = Precedence(stackedOperator.Text);
        var incomingPrecedence = Precedence(incomingOperator.Text);
        var incomingIsRightAssociative = incomingOperator.Text == "^";

        return stackedPrecedence > incomingPrecedence ||
               (stackedPrecedence == incomingPrecedence && !incomingIsRightAssociative);
    }

    private static int Precedence(string op) => op switch
    {
        "+" or "-" => 1,
        "*" or "/" => 2,
        "u-" => 3,
        "^" => 4,
        _ => throw new InvalidOperationException($"Operador interno no reconocido: {op}"),
    };

    private static double EvaluatePostfix(List<Token> postfix)
    {
        var values = new Stack<double>();

        foreach (var token in postfix)
        {
            if (token.Type == TokenType.Number)
            {
                values.Push(token.Value);
                continue;
            }

            if (token.Text == "u-")
            {
                if (values.Count < 1)
                {
                    throw new FormatException("El signo negativo no tiene un operando.");
                }

                values.Push(-values.Pop());
                continue;
            }

            if (values.Count < 2)
            {
                throw new FormatException($"El operador '{token.Text}' no tiene suficientes operandos.");
            }

            var right = values.Pop();
            var left = values.Pop();
            values.Push(Calculate(left, right, token.Text));
        }

        if (values.Count != 1)
        {
            throw new FormatException("La expresión no forma una operación válida.");
        }

        return values.Pop();
    }

    private static double Calculate(double left, double right, string op)
    {
        if (op == "/" && right == 0)
        {
            throw new DivideByZeroException("No se puede dividir por cero.");
        }

        var result = op switch
        {
            "*" => left * right,
            "/" => left / right,
            "+" => left + right,
            "-" => left - right,
            "^" => Math.Pow(left, right),
            _ => throw new InvalidOperationException($"Operador interno no reconocido: {op}"),
        };

        if (!double.IsFinite(result))
        {
            throw new ArithmeticException("El resultado no es un número real finito.");
        }

        return result;
    }

    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';
}
