namespace Checkers.Shared.Models
{
    public class MoveDto
    {
        public int Sr { get; set; }
        public int Sc { get; set; }
        public int Tr { get; set; }
        public int Tc { get; set; }
        public bool Capture { get; set; }

        public MoveDto() { }

        public MoveDto(int sr, int sc, int tr, int tc, bool capture)
        {
            Sr = sr;
            Sc = sc;
            Tr = tr;
            Tc = tc;
            Capture = capture;
        }
    }
}
