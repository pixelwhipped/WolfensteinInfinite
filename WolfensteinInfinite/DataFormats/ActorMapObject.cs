//Clean
namespace WolfensteinInfinite.DataFormats
{
    public struct ActorMapObject
    {
        public int X;
        public int Y;
        public int Angle;
        public string ActorType;
        public ActorMapObject(int x, int y, int angle, string id)
        {
            X = x;
            Y = y;
            ActorType = id;
            Angle = angle;

            if (Angle < 0)
            {
                Angle += 360; // Ensure angle is positive
            }
            else if (Angle >= 360)
            {
                Angle -= 360; // Ensure angle is within 0-359 degrees
            }
        }
    }
}