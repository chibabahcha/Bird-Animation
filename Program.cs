using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;

namespace BirdPlatformer
{
    enum Direction
    {
        Down,
        Left,
        Right,
        Up
    }

    enum GameState
    {
        Menu,
        Playing,
        Paused
    }

    static class MathHelper
    {
        public static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0, 1);
    }

    class Player
    {
        public Sprite Sprite { get; private set; }
        public Vector2f Position { get; set; }
        public Vector2f Velocity { get; set; }
        public bool IsGrounded { get; set; }
        private Texture Texture;
        private Dictionary<Direction, (float speed, int[] frames)> Animations;
        private int CurrentFrame;
        private float AnimationTimer;
        private const float AnimationSpeedBase = 0.1f;
        private Direction CurrentDirection;

        public Player(Texture texture, Vector2f initialPosition)
        {
            Texture = texture;
            Sprite = new Sprite(texture);
            Position = initialPosition;
            Velocity = new Vector2f(0, 0);
            IsGrounded = false;

            Animations = new Dictionary<Direction, (float speed, int[])>
            {
                [Direction.Down] = (AnimationSpeedBase, new[] { 0, 1, 2 }),
                [Direction.Left] = (AnimationSpeedBase * 0.8f, new[] { 3, 4, 5 }),
                [Direction.Right] = (AnimationSpeedBase * 0.8f, new[] { 6, 7, 8 }),
                [Direction.Up] = (AnimationSpeedBase * 1.2f, new[] { 9, 10, 11 })
            };
            CurrentDirection = Direction.Down;
            CurrentFrame = 0;
            AnimationTimer = 0f;
        }

        public void Update(float deltaTime, List<Platform> platforms)
        {
            // Физика
            Velocity = new Vector2f(Velocity.X, Velocity.Y + 900f * deltaTime); // Гравитация
            Position = Position + Velocity * deltaTime;

            // Коллизии
            FloatRect playerBounds = new FloatRect(Position, new Vector2f(61, 57));
            IsGrounded = false;

            foreach (var platform in platforms)
            {
                FloatRect platformBounds = new FloatRect(platform.Shape.Position, platform.Shape.Size);
                if (playerBounds.Intersects(platformBounds))
                {
                    Vector2f overlap = new Vector2f(
                        Math.Min(playerBounds.Left + playerBounds.Width - platformBounds.Left, platformBounds.Left + platformBounds.Width - playerBounds.Left),
                        Math.Min(playerBounds.Top + playerBounds.Height - platformBounds.Top, platformBounds.Top + platformBounds.Height - playerBounds.Top)
                    );

                    if (Math.Abs(overlap.X) < Math.Abs(overlap.Y))
                    {
                        Position = new Vector2f(Position.X + (overlap.X > 0 ? -overlap.X : overlap.X), Position.Y);
                        Velocity = new Vector2f(0, Velocity.Y);
                    }
                    else
                    {
                        Position = new Vector2f(Position.X, Position.Y + (overlap.Y > 0 ? -overlap.Y : overlap.Y));
                        Velocity = new Vector2f(Velocity.X, 0);
                        if (overlap.Y > 0) IsGrounded = true;
                    }
                }
            }

            // Анимация
            (float speed, int[] frames) = Animations[CurrentDirection];
            AnimationTimer += deltaTime;
            if (AnimationTimer >= speed)
            {
                AnimationTimer = 0f;
                CurrentFrame = (CurrentFrame + 1) % frames.Length;
            }

            // Направление
            float moveX = 0;
            if (Keyboard.IsKeyPressed(Keyboard.Key.A)) moveX -= 1;
            if (Keyboard.IsKeyPressed(Keyboard.Key.D)) moveX += 1;

            if (moveX < 0) CurrentDirection = Direction.Left;
            else if (moveX > 0) CurrentDirection = Direction.Right;
            else if (Velocity.Y < 0) CurrentDirection = Direction.Up;
            else CurrentDirection = Direction.Down;

            // Управление
            Velocity = new Vector2f(moveX * 350f, Velocity.Y);
            if (Keyboard.IsKeyPressed(Keyboard.Key.Space) && IsGrounded)
            {
                Velocity = new Vector2f(Velocity.X, -450f);
                IsGrounded = false;
            }

            // Зеркальное отражение
            Sprite.Scale = CurrentDirection == Direction.Left ? new Vector2f(-1, 1) : new Vector2f(1, 1);
            Sprite.Position = Position + (CurrentDirection == Direction.Left ? new Vector2f(61, 0) : new Vector2f(0, 0));

            // Текстура
            int frameY = Array.IndexOf(Animations[CurrentDirection].frames, Animations[CurrentDirection].frames[CurrentFrame]);
            Sprite.TextureRect = new IntRect(
                Animations[CurrentDirection].frames[CurrentFrame] % 3 * 61,
                frameY / 3 * 57,
                61,
                57
            );
        }

        public void Draw(RenderWindow window)
        {
            window.Draw(Sprite);

            // Тень под орлом
            RectangleShape shadow = new RectangleShape(new Vector2f(61, 10))
            {
                Position = Position + new Vector2f(0, 57 - 5),
                FillColor = new Color(0, 0, 0, 100)
            };
            window.Draw(shadow);
        }
    }

    class Platform
    {
        public RectangleShape Shape;

        public Platform(Vector2f position, Vector2f size)
        {
            Shape = new RectangleShape(size);
            Shape.Position = position;
            Shape.FillColor = new Color(100, 100, 200);
        }

        public void Draw(RenderWindow window)
        {
            window.Draw(Shape);

            // Тень под платформой
            RectangleShape shadow = new RectangleShape(new Vector2f(Shape.Size.X, 5))
            {
                Position = Shape.Position + new Vector2f(0, Shape.Size.Y - 5),
                FillColor = new Color(0, 0, 0, 80)
            };
            window.Draw(shadow);
        }
    }

    class Enemy
    {
        public RectangleShape Shape;
        public float Speed = 100f;
        public float JumpForce = -300f;
        private float PatrolRange = 200f;
        private Vector2f PatrolCenter;

        public Enemy(Vector2f position)
        {
            Shape = new RectangleShape(new Vector2f(40, 40));
            Shape.Position = position;
            Shape.FillColor = Color.Red;
            PatrolCenter = position;
        }

        public void Update(Vector2f playerPosition, List<Platform> platforms, float deltaTime)
        {
            Vector2f direction = playerPosition - Shape.Position;
            float length = (float)Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

            if (length > 500) // Патрулирование
            {
                Shape.Position = new Vector2f(
                    PatrolCenter.X + (float)Math.Sin(new Clock().ElapsedTime.AsSeconds() * 2) * PatrolRange / 2,
                    Shape.Position.Y
                );
            }
            else if (length > 50) // Преследование
            {
                direction /= length;
                Shape.Position += direction * Speed * deltaTime;
            }

            // Проверка прыжка
            bool onGround = false;
            Vector2f enemyBottom = Shape.Position + new Vector2f(0, 40);
            foreach (var platform in platforms)
            {
                FloatRect enemyBounds = new FloatRect(Shape.Position, new Vector2f(40, 40));
                FloatRect platformBounds = new FloatRect(platform.Shape.Position, platform.Shape.Size);

                if (enemyBounds.Intersects(platformBounds))
                {
                    if (enemyBounds.Top + enemyBounds.Height - platformBounds.Top < 5)
                    {
                        onGround = true;
                        break;
                    }
                }
            }

            if (!onGround && playerPosition.Y < Shape.Position.Y - 100)
            {
                float distanceToNearestPlatform = float.MaxValue;
                Platform nearestPlatform = null;
                foreach (var platform in platforms)
                {
                    FloatRect platformBounds = new FloatRect(platform.Shape.Position, platform.Shape.Size);
                    float dist = platformBounds.Top - (Shape.Position.Y + 40);
                    if (dist > 0 && dist < distanceToNearestPlatform && platformBounds.Left < Shape.Position.X + 40 && platformBounds.Left + platform.Shape.Size.X > Shape.Position.X)
                    {
                        distanceToNearestPlatform = dist;
                        nearestPlatform = platform;
                    }
                }
                if (distanceToNearestPlatform < 100)
                {
                    Vector2f velocity = new Vector2f(0, JumpForce);
                    Shape.Position += velocity * deltaTime;
                }
            }
        }

        public void Draw(RenderWindow window)
        {
            window.Draw(Shape);

            // Тень под врагом
            RectangleShape shadow = new RectangleShape(new Vector2f(40, 5))
            {
                Position = Shape.Position + new Vector2f(0, 40 - 5),
                FillColor = new Color(0, 0, 0, 80)
            };
            window.Draw(shadow);
        }
    }

    class ParticleSystem
    {
        private List<Particle> Particles = new List<Particle>();
        private Random Random = new Random(); // Перенёс Random сюда, теперь не статический

        public void AddDust(Vector2f position, int count = 10)
        {
            for (int i = 0; i < count; i++)
            {
                byte r = (byte)Random.Next(0, 256);
                byte g = (byte)Random.Next(0, 256);
                byte b = (byte)Random.Next(0, 256);
                Particles.Add(new Particle(position + new Vector2f(30.5f, 57), new Color(r, g, b), 5f, 0.5f));
            }
        }

        public void Update(float deltaTime)
        {
            Particles.RemoveAll(p => p.Update(deltaTime));
        }

        public void Draw(RenderWindow window)
        {
            foreach (var particle in Particles)
            {
                window.Draw(particle.Shape);
            }
        }
    }

    class Particle
    {
        public CircleShape Shape;
        public Vector2f Velocity;
        public float Lifetime;
        public float MaxLifetime;
        private static Random Random = new Random(); // Статический Random для всех частиц

        public Particle(Vector2f position, Color color, float size, float lifetime)
        {
            Shape = new CircleShape(size);
            Shape.Position = position - new Vector2f(size, size);
            Shape.FillColor = color;
            Velocity = new Vector2f((float)(Random.NextDouble() * 100 - 50), (float)(Random.NextDouble() * 100 - 50)); // Теперь работает
            MaxLifetime = lifetime;
            Lifetime = lifetime;
        }

        public bool Update(float deltaTime)
        {
            Lifetime -= deltaTime;
            Shape.Position += Velocity * deltaTime;
            Shape.FillColor = new Color(
                Shape.FillColor.R,
                Shape.FillColor.G,
                Shape.FillColor.B,
                (byte)(255 * (Lifetime / MaxLifetime))
            );
            return Lifetime <= 0;
        }
    }

    class Program
    {
        static void Main()
        {
            RenderWindow window = new RenderWindow(new VideoMode(800, 600), "Bird Platformer - Final");
            window.SetFramerateLimit(60);

            // Загрузка текстуры
            Texture texture;
            try
            {
                texture = new Texture("C:\\Users\\miron\\RiderProjects\\SFML2025\\SFML2025\\Resources\\Images\\Sprites\\Enemy\\bird1_61x57.png");
                Console.WriteLine($"Текстура орла загружена. Размеры: {texture.Size.X}x{texture.Size.Y} пикселей");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки текстуры орла: {ex.Message}");
                return;
            }

            Vector2f worldSize = new Vector2f(1600, 1200);

            // Процедурная генерация платформ
            Random random = new Random();
            List<Platform> platforms = new List<Platform>
            {
                new Platform(new Vector2f(0, worldSize.Y - 50), new Vector2f(worldSize.X, 50))
            };
            for (int i = 0; i < 10; i++)
            {
                float x = random.Next(0, (int)worldSize.X - 200);
                float y = random.Next(200, (int)worldSize.Y - 100);
                platforms.Add(new Platform(new Vector2f(x, y), new Vector2f(200, 20)));
            }

            // Создание игрока, врагов и частиц
            Player player = new Player(texture, new Vector2f(400, 300));
            List<Enemy> enemies = new List<Enemy>
            {
                new Enemy(new Vector2f(1000, 260)),
                new Enemy(new Vector2f(1200, 360)),
                new Enemy(new Vector2f(800, 450))
            };
            ParticleSystem particleSystem = new ParticleSystem();

            // Сохранение/загрузка
            string saveFile = "save.txt";
            if (File.Exists(saveFile))
            {
                string[] lines = File.ReadAllLines(saveFile);
                player.Position = new Vector2f(float.Parse(lines[0]), float.Parse(lines[1]));
            }

            // Камера
            View camera = new View(new Vector2f(400, 300), new Vector2f(800, 600));
            float cameraSmoothness = 5f;

            GameState gameState = GameState.Playing;
            Clock clock = new Clock();

            while (window.IsOpen)
            {
                window.DispatchEvents();
                if (Keyboard.IsKeyPressed(Keyboard.Key.Escape))
                {
                    File.WriteAllLines(saveFile, new[] { player.Position.X.ToString(), player.Position.Y.ToString() });
                    window.Close();
                }

                float deltaTime = clock.Restart().AsSeconds();

                if (gameState != GameState.Playing)
                {
                    if (Keyboard.IsKeyPressed(Keyboard.Key.P))
                        gameState = GameState.Playing;

                    window.Clear(new Color(40, 40, 40));
                    foreach (Platform platform in platforms) platform.Draw(window);
                    foreach (Enemy enemy in enemies) enemy.Draw(window);
                    player.Draw(window);
                    window.Display();
                    continue;
                }

                if (Keyboard.IsKeyPressed(Keyboard.Key.P))
                    gameState = GameState.Paused;

                // Обновление игрока
                player.Update(deltaTime, platforms);

                // Обновление врагов
                foreach (Enemy enemy in enemies)
                    enemy.Update(player.Position, platforms, deltaTime);

                // Визуальные эффекты
                if (!player.IsGrounded && player.Velocity.Y < 0)
                    particleSystem.AddDust(player.Position + new Vector2f(30.5f, 57));
                particleSystem.Update(deltaTime);

                // Камера
                Vector2f targetCameraPos = player.Position + new Vector2f(30.5f, 28.5f);
                camera.Center = new Vector2f(
                    MathHelper.Lerp(camera.Center.X, targetCameraPos.X, cameraSmoothness * deltaTime),
                    MathHelper.Lerp(camera.Center.Y, targetCameraPos.Y, cameraSmoothness * deltaTime)
                );
                camera.Center = new Vector2f(
                    Math.Clamp(camera.Center.X, 400, worldSize.X - 400),
                    Math.Clamp(camera.Center.Y, 300, worldSize.Y - 300)
                );
                window.SetView(camera);

                // Отрисовка
                window.Clear(new Color(40, 40, 40));
                foreach (Platform platform in platforms) platform.Draw(window);
                particleSystem.Draw(window);
                foreach (Enemy enemy in enemies) enemy.Draw(window);
                player.Draw(window);
                window.Display();
            }
        }
    }
}
