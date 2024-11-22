namespace IniParserTests;

sealed class RecordBuilder
{
    public static RecordBuilder New => new();

    private readonly List<Action<string>> _asserts = [];

    private RecordBuilder Add(string txt)
    {
        _asserts.Add(c => Assert.Equal(txt, c));
        return this;
    }

    public RecordBuilder StartSection(ReadOnlySpan<char> name) => Add($"StartSection({name})");
    public RecordBuilder EndSection(ReadOnlySpan<char> name) => Add($"EndSection({name})");
    public RecordBuilder ReadValue(ReadOnlySpan<char> section, ReadOnlySpan<char> key, ReadOnlySpan<char> value) => Add($"ReadValue({section}, {key}, {value})");
    public void Check(IEnumerable<string> values) => Assert.Collection(values, _asserts.ToArray());
}