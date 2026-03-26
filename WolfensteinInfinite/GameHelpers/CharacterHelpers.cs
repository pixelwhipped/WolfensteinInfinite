//Clean
using WolfensteinInfinite.Engine.Graphics;
using WolfensteinInfinite.GameBible;
using WolfensteinInfinite.GameGraphics;
using WolfensteinInfinite.Utilities;

namespace WolfensteinInfinite.GameHelpers
{
    public static class CharacterHelpers
    {
        public static CharacterSprite ReadChatacterAnimations(string path, int start, CharacterSpriteType type)
        {
            Dictionary<CharacterAnimationState, Animation> animations = [];
            path = FileHelpers.Shared.GetModDataFilePath(path);
            switch (type)
            {
                case CharacterSpriteType.GHOST:
                    ReadGhostAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.GUARD:
                    ReadGuardAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.DOG:
                    ReadDogAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.MUTANT:
                    ReadMutantAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.OFFICER:
                    ReadOfficerAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.BOSS:
                    ReadBossAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.DOCTOR_SCHABBS:
                    ReadDoctorAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.MECHA_HITLER:
                    ReadMechaAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.ADOLF_HITLER:
                    ReadAdolfAnimations(animations, path, start);
                    break;
                case CharacterSpriteType.HITLER_GHOST:
                    ReadGhostHitlerAnimations(animations, path, start);
                    break;
            }
            return new CharacterSprite(animations);
        }
        private static void ReadGhostHitlerAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 3, 1));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 3, 3.5f));
            animation.Clear();
            //i = end;
            end += 2;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 2, 3.5f));
            animation.Clear();
            i += 2;
            end = i + 5;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 5, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 5, 3.5f) { Loop = false });
            animation.Clear();
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
        }
        private static void ReadAdolfAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 4, 1));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 4, 3.5f));
            animation.Clear();
            //i = end;
            end += 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
            animation.Clear();
            //i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });

            i++;
            end = i + 7;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 8, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 8, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0],animation[0]], 1, 2, 8f) { Loop = false });
            animation.Clear();

        }
        private static void ReadMechaAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 4, 1));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 4, 3.5f));
            animation.Clear();
            //i = end;
            end += 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
            animation.Clear();
            //i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            animation.Clear();
            i++;
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });

        }
        private static void ReadDoctorAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 4, 1));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 4, 3.5f));
            animation.Clear();
            //i = end;
            end += 2;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 2, 3.5f));
            animation.Clear();

            //i = end;
            end += 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
            animation.Clear();
            //i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
        }
        private static void ReadBossAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 4, 3.5f));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 4, 3.5f));
            animation.Clear();
            i = end;
            end += 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
            animation.Clear();
            //i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            animation.Clear();
            i++;
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
        }
        private static void ReadOfficerAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 8;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 8, 1, 1));
            animation.Clear();
            i = end;
            end += 8 * 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 8, 4, 3.5f));
            animation.Clear();
            //i = end;
            end += 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 5, 3.5f) { Loop = false });
            animation.Clear();
            i--;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            i -= 3;
            for (; i < end; i++)
            {
                var t = FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png"));
                t = FlipTexture(t);
                animation.Add(t);
            }
            i++;
            animation.Add(FlipTexture(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png"))));
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 5, 3.5f) { Loop = false });
            i++;
            animation.Clear();
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
            i++;
            animation.Clear();
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
        }
        private static void ReadMutantAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 8;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 8, 1, 1));
            animation.Clear();
            i = end;
            end += 8 * 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 8, 4, 3.5f));
            animation.Clear();
            //i = end;

            end += 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 5, 3.5f) { Loop = false });
            animation.Clear();
            i--;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            i -= 3;
            for (; i < end; i++)
            {
                var t = FlipTexture(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
                animation.Add(t);
            }
            i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 5, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
            animation.Clear();
            i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            animation.Clear();
            i++;
            end = i + 4;
            for (; i < end; i++)
            {
                var t = FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png"));
                animation.Add(t);
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 4, 3.5f));
        }
        private static void ReadDogAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>
            {
                FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{start + 16}.png")) //115
            };
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 1, 1));
            int i = start;
            int end = start + 8 * 4;
            animation.Clear();
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 8, 4, 3.5f));
            animation.Clear();
            i = end;
            end += 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
            animation.Clear();
            i -= 3;
            for (; i < end; i++)
            {
                var t = FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png"));
                t = FlipTexture(t);
                animation.Add(t);
            }
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });

            animation.Clear();
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
            i++;
            animation.Clear();
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
        }
        private static void ReadGuardAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            int i = start;
            int end = start + 8;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 8, 1, 1));
            animation.Clear();
            i = end;
            end += 8 * 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 8, 4, 3.5f));
            animation.Clear();
            i = end;
            end += 4;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 4, 3.5f) { Loop = false });
            animation.Clear();
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            i -= 3;
            //end--;

            for (; i < end; i++)
            {
                var t = FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png"));
                t = FlipTexture(t);
                animation.Add(t);
            }
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 4, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0],animation[0]], 1, 2, 8f) { Loop = false });
            i++;
            animation.Clear();
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
            i++;
            animation.Clear();
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
        }
        private static void ReadGhostAnimations(Dictionary<CharacterAnimationState, Animation> Animations, string path, int start)
        {
            var animation = new List<Texture32>();
            for (var i = start; i < start + 2; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 2, 3.5f));
            Animations.Add(CharacterAnimationState.WALKING, new Animation([.. animation], 1, 2, 3.5f));
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 2, 3.5f));
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 2, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DYING_RIGHT, new Animation([.. animation], 1, 2, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 2, 3.5f) { Loop = false });
            Animations.Add(CharacterAnimationState.HIT, new Animation([animation[0], animation[0]], 1, 2, 8f) { Loop = false });
        }
        private static Texture32 FlipTexture(Texture32 t)
        {
            var ret = new Texture32(t.Width, t.Height);
            for (int y = 0; y < t.Height; y++)
            {
                int xd = 0;
                for (int x = t.Width - 1; x >= 0; x--)
                {
                    ret.PutPixel(xd, y, t.GetPixel(x, y));
                    xd++;
                }
            }
            return ret;
        }
    }
}
