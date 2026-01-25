namespace MajdataEdit.MaiMuriDX;

public class RunArg
{
    public RunArg(string fumen, float offset, string trackPath, bool render) 
    {
        this.fumen = fumen;
        this.offset = offset;
        this.track = trackPath;
        this.render = render;
    }

    public string fumen { get; set; }
    public float offset { get; set; }
    public string track { get; set; }
    public bool render { get; set; }
}
