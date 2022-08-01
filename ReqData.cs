namespace TVProfilSched;
class ReqData
{
    public string datum, kanal, bCodeName;
    public int bCode;

    public ReqData(string date, string channel)
    {
        datum = date;
        kanal = channel;

        string a = datum + kanal;
        string ua = kanal + datum;
        int i = a.Length;
        int b = 2;
        int c = 2 + ua.Sum(c => c);

        while (i-- > 0)
            b += (a[i] + (c * 2)) * i;

        bCode = b;
        bCodeName = "b" + (int)b.ToString()[2];
    }
}