namespace IniParser;

public sealed class IniStreamParser
{
    public interface IReceiver
    {
        void StartSection(ReadOnlySpan<char> sectionName);
        void EndSection(ReadOnlySpan<char> sectionName);
        void ReadValue(ReadOnlySpan<char> sectionName, ReadOnlySpan<char> key, ReadOnlySpan<char> value);
        void Error(int position, int length, string errorMessage);
    }

    private int _cursor;
    private readonly TextReader _reader;
    private readonly IReceiver _receiver;

    public static void Read(TextReader reader, IReceiver receiver)
    {
        new IniStreamParser(reader, receiver).Read();
    }

    public static void Read(string str, IReceiver receiver) => Read(new StringReader(str), receiver);

    private void Read()
    {
        NextToken();
        while (TryReadSection() || TryReadKeyValuePair(null)) { }
    }


    private enum Token
    {
        EndOfFile,
        Identifier,
        Equal,
        BracketOpen,
        BracketClose,
        IncompleteString,
    }

    /// Position of the currentToken
    private int _position;
    /// The current token type
    private Token _token;
    /// The string value of the current token, only for Identifier
    private TempString _valueToken = TempString.Create();
    private TempString _valueSection = TempString.Create();
    private TempString _valueKey = TempString.Create();
    private TempString _valueValue = TempString.Create();

    private struct TempString(char[] value, int length)
    {
        private char[] _value = value;
        private int _length = length;

        public static TempString Create() => new(new char[32], 0);

        public void Add(char c)
        {
            if (_length >= _value.Length)
                Array.Resize(ref _value, Math.Max(_value.Length * 2, _length + 1));
            _value[_length++] = c;
        }

        public void Clear()
        {
            _length = 0;
        }
        
        public readonly ReadOnlySpan<char> AsSpan() => _value.AsSpan(0, _length);
        public override string ToString() => AsSpan().ToString();
    }

    private IniStreamParser(TextReader reader, IReceiver receiver)
    {
        _reader = reader;
        _receiver = receiver;
    }

    private void NextToken()
    {
        _position = _cursor;
        _token = NextToken(ref _valueToken);
    }
    
    private Token NextToken(ref TempString valueToken)
    {
        valueToken.Clear();
        bool inComment = false;
        char c;
        do {
            if (!TryRead(out c))
                return Token.EndOfFile;
            inComment = inComment ? c != '\n' : c == '#';
        } while (inComment || char.IsWhiteSpace(c));

        switch (c) {
            case '=':
                return Token.Equal;
            case '[':
                return Token.BracketOpen;
            case ']':
                return Token.BracketClose;
            case '"': {
                bool inEscape = false;
                while (TryRead(out c)) {
                    if (inEscape) {
                        valueToken.Add(c);
                        inEscape = false;
                    } else if (c == '"') {
                        return Token.Identifier;
                    } else if (c == '\\') {
                        inEscape = true;
                    } else {
                        valueToken.Add(c);
                    }
                }

                return Token.IncompleteString;
            }
            default:
                valueToken.Add(c);
                while (TryReadIdentifier(out c))
                    valueToken.Add(c);

                return Token.Identifier;
        }

        static bool TryToChar(int ic, out char c)
        {
            c = unchecked((char)ic);
            return ic != -1;
        }

        bool TryRead(out char c)
        {
            _cursor++;
            return TryToChar(_reader.Read(), out c);
        }

        bool TryReadIdentifier(out char c)
        {
            if (TryToChar(_reader.Peek(), out c) && !char.IsWhiteSpace(c) && c != '[' && c != ']' && c != '=' && c != '"' && c != '#')
            {
                _cursor++;
                _reader.Read();
                return true;
            }

            return false;
        }
    }

    private bool TryRead(Token t)
    {
        if (t != _token) return false;
        NextToken();
        return true;
    }

    private void Expect(Token c)
    {
        if(!TryRead(c))
            Error(c);
    }
    private void ReadIdentifier(ref TempString identifier)
    {
        if (!TryReadIdentifier(ref identifier))
            Error(Token.Identifier);
    }

    private void Error(Token c)
    {
        var error = _token switch
        {
            Token.Identifier => $"Expected {c} but found Identifier({_valueToken})",
            _ => $"Expected {c} but found {_token}"
        };
        _receiver.Error(_position, _cursor - _position, error);
    }

    private bool TryReadIdentifier(ref TempString identifier)
    {
        if (_token != Token.Identifier) return false;
        // Give the user our current working space.
        // And use the now unused user data for the working space.
        // Efficient (:
        (_valueToken, identifier) = (identifier, _valueToken);
        NextToken();
        return true;
    }
    
    private bool TryReadSection()
    {
        if (!TryRead(Token.BracketOpen)) return false;
        ReadIdentifier(ref _valueSection);
        Expect(Token.BracketClose);
        var sectionSpan = _valueSection.AsSpan();
        _receiver.StartSection(sectionSpan);
        while (TryReadKeyValuePair(sectionSpan)) { }
        _receiver.EndSection(sectionSpan);
        return true;
    }

    private bool TryReadKeyValuePair(ReadOnlySpan<char> section)
    {
        if (!TryReadIdentifier(ref _valueKey)) return false;
        Expect(Token.Equal);
        ReadIdentifier(ref _valueValue);
        _receiver.ReadValue(section, _valueKey.AsSpan(), _valueValue.AsSpan());
        return true;
    }
}
