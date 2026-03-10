using WolfensteinInfinite.GameBible;

namespace WolfensteinInfinite
{
    public class CharacterSprite : ISprite
    {
        private readonly Dictionary<CharacterAnimationState, Animation> Animations = [];
        public CharacterAnimationState AnimationState { get; set; } = CharacterAnimationState.STANDING;
        public CharacterSprite(Dictionary<CharacterAnimationState, Animation> animations) => Animations = animations;
        public CharacterSprite(string path, int start, CharacterSpriteType type)
        {
            path = FileHelpers.Shared.GetModDataFilePath(path);
            switch (type)
            {
                case CharacterSpriteType.GHOST:
                    ReadGhostAnimations(path, start);
                    break;
                case CharacterSpriteType.GUARD:
                    ReadGuardAnimations(path, start);
                    break;
                case CharacterSpriteType.DOG:
                    ReadDogAnimations(path, start);
                    break;
                case CharacterSpriteType.MUTANT:
                    ReadMutantAnimations(path, start);
                    break;
                case CharacterSpriteType.OFFICER:
                    ReadOfficerAnimations(path, start);
                    break;
                case CharacterSpriteType.BOSS:
                    ReadBossAnimations(path, start);
                    break;
                case CharacterSpriteType.DOCTOR_SCHABBS:
                    ReadDoctorAnimations(path, start);
                    break;
                case CharacterSpriteType.MECHA_HITLER:
                    ReadMechaAnimations(path, start);
                    break;
                case CharacterSpriteType.ADOLF_HITLER:
                    ReadAdolfAnimations(path, start);
                    break;
                case CharacterSpriteType.HITLER_GHOST:
                    ReadGhostHitlerAnimations(path, start);
                    break;
            }

        }
        public void Update(float frameTimeSeconds)
        {
            foreach (var a in Animations.Values)
                a.Update(frameTimeSeconds);
        }

        public bool IsDeathAnimationComplete =>
            (AnimationState == CharacterAnimationState.DYING_LEFT ||
             AnimationState == CharacterAnimationState.DYING_RIGHT) &&
             Animations[AnimationState].IsComplete; // need to verify Animation API
        public Texture32 GetTexture(float angle)
        {
            return Animations[AnimationState].GetTexture(angle);
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
        private void ReadGhostHitlerAnimations(string path, int start)
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
        }
        private void ReadAdolfAnimations(string path, int start)
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
            animation.Clear();

        }
        private void ReadMechaAnimations(string path, int start)
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

        }
        private void ReadDoctorAnimations(string path, int start)
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
            animation.Clear();
            //i++;
            animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            Animations.Add(CharacterAnimationState.DEAD, new Animation([.. animation], 1, 1, 1) { Loop = false });
        }
        private void ReadBossAnimations(string path, int start)
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
        }
        private void ReadOfficerAnimations(string path, int start)
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
            end += (8 * 4);
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
            i++;
            animation.Clear();
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
        }
        private void ReadMutantAnimations(string path, int start)
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
            end += (8 * 4);
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
        private void ReadDogAnimations(string path, int start)
        {
            var animation = new List<Texture32>
            {
                FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{start + 16}.png")) //115
            };
            Animations.Add(CharacterAnimationState.STANDING, new Animation([.. animation], 1, 1, 1));
            int i = start;
            int end = start + (8 * 4);
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
            Animations.Add(CharacterAnimationState.DYING_LEFT, new Animation([.. animation], 1, 3, 3.5f));
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
            i++;
            animation.Clear();
            end = i + 3;
            for (; i < end; i++)
            {
                animation.Add(FileHelpers.Shared.LoadSurface32(System.IO.Path.Combine(path, $"{i}.png")));
            }
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f));
        }
        private void ReadGuardAnimations(string path, int start)
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
            end += (8 * 4);
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
            Animations.Add(CharacterAnimationState.ATTACKING, new Animation([.. animation], 1, 3, 3.5f) { Loop = false });
        }
        private void ReadGhostAnimations(string path, int start)
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
        }

    }
}
