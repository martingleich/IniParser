using IniParser;

namespace IniParserTests;

public class UnitTests
{
    private static TI SimpleReader<TR, TI>(string value, Func<(TR, Func<TR, TI>)> receiver) where TR:IniStreamParser.IReceiver
    {
        var (r, m) = receiver();
        IniStreamParser.Read(value, r);
        return m(r);
    }
    [Fact]
    public void EmptyString()
    {
        var data = SimpleReader("", EventRecordReceiver.Factory);
        RecordBuilder.New.Check(data);
    }
    [Fact]
    public void EmptySection()
    {
        var data = SimpleReader("[Display]", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .EndSection("Display")
            .Check(data);
    }
    [Fact]
    public void LongNames()
    {
        var lA = new string('A', 3253);
        var lB = new string('B', 1724);
        var lC = new string('C', 2435);
        var data = SimpleReader($"[{lA}]{lB}={lC}", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection(lA)
            .ReadValue(lA, lB, lC)
            .EndSection(lA)
            .Check(data); }
    [Fact]
    public void SectionWithTwoValues()
    {
        var data = SimpleReader("[Display]\nabc=100\nblub=hello", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .ReadValue("Display", "blub", "hello")
            .EndSection("Display")
            .Check(data);
    }
    [Fact]
    public void TwoSections()
    {
        var data = SimpleReader("[Display]\nabc=100\n[Other]\nblub=hello", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .EndSection("Display")
            .StartSection("Other")
            .ReadValue("Other", "blub", "hello")
            .EndSection("Other")
            .Check(data);
    }
    [Fact]
    public void MinimalWhitespace()
    {
        var data = SimpleReader("[Display]abc =100\t[Other]blub= hello", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .EndSection("Display")
            .StartSection("Other")
            .ReadValue("Other", "blub", "hello")
            .EndSection("Other")
            .Check(data);
    }
    [Fact]
    public void DuplicateSection()
    {
        var data = SimpleReader("[Display]abc =100\t[Display]blub= hello", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .EndSection("Display")
            .StartSection("Display")
            .ReadValue("Display", "blub", "hello")
            .EndSection("Display")
            .Check(data);
    }
    [Fact]
    public void DuplicateKey()
    {
        var data = SimpleReader("[Display]abc =100\tabc= hello", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .ReadValue("Display", "abc", "hello")
            .EndSection("Display")
            .Check(data);
    }
    [Fact]
    public void VariousComments()
    {
        var data = SimpleReader("# Hello\n[Display] # Blub\nabc=\n#Test\n100\nblub#Test\n=hello #Trailing", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .ReadValue("Display", "blub", "hello")
            .EndSection("Display")
            .Check(data);
    }
    [Fact]
    public void SpaceInIdentifier()
    {
        var data = SimpleReader("# Hello\r\n[\"Display a\"] \"\\\\\"=\"\\\"\"", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display a")
            .ReadValue("Display a", "\\", "\"")
            .EndSection("Display a")
            .Check(data);
    }
    [Fact]
    public void CommentWindows()
    {
        var data = SimpleReader("# Hello\r\n[Display] abc=100\nblub=hello", EventRecordReceiver.Factory);
        RecordBuilder.New
            .StartSection("Display")
            .ReadValue("Display", "abc", "100")
            .ReadValue("Display", "blub", "hello")
            .EndSection("Display")
            .Check(data);
    }

    [Theory]
    [InlineData("[Display")]
    [InlineData("[Display] a 5")]
    [InlineData("[\"Display] a 5")]
    [InlineData("[Display %/-")]
    public void Error(string code)
    {
        Assert.Throws<ParserException>(() => SimpleReader(code , EventRecordReceiver.Factory));
    }
}