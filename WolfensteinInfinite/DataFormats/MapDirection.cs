//Clean
namespace WolfensteinInfinite.DataFormats
{
    [Flags]
    public enum MapDirection
    {
        DIR_NONE = 0,
        DIR_NORTH = 1 << 0,
        DIR_SOUTH = 1 << 1,
        DIR_EAST = 1 << 2,
        DIR_WEST = 1 << 3
    }
}