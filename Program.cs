using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;

class Program
{
    enum Direction
    {
        Down,
        Left,
        Right,
        Up
    }

    static class MathHelper
    {
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0, 1);
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
    }

    static void Main()
    {
        RenderWindow window = new RenderWindow(new VideoMode(800, 600), "Platformer");
        window.SetFramerateLimit(60);

        Texture texture = new Texture("C:\\Users\\miron\\RiderProjects\\SFML2025\\SFML2025\\assets\\eagle.png");
        Sprite sprite = new Sprite(texture);
        Vector2f worldSize = new Vector2f(1600, 1200);
        
        // Инициализация платформ
        List<Platform> platforms = new List<Platform>
        {
            new Platform(new Vector2f(0, worldSize.Y - 50), new Vector2f(worldSize.X, 50)), // Пол
            new Platform(new Vector2f(300, 500), new Vector2f(200, 20)),
            new Platform(new Vector2f(600, 400), new Vector2f(200, 20)),
            new Platform(new Vector2f(900, 300), new Vector2f(200, 20))
        };

        int frameWidth = 61;
        int frameHeight = 57;
        int framesPerRow = 3;
        int framesPerColumn = 4;

        Direction currentDirection = Direction.Down;
        int currentFrame = 0;
        float animationSpeed = 0.1f;
        float animationTimer = 0f;
        Clock clock = new Clock();

        Vector2f spritePosition = new Vector2f(400, 300);
        Vector2f velocity = new Vector2f();
        float gravity = 900f;
        float jumpForce = -450f;
        float movementSpeed = 350f;
        bool isGrounded = false;

        View camera = new View(new Vector2f(400, 300), new Vector2f(800, 600));
        float cameraSmoothness = 5f;

        while (window.IsOpen)
        {
            window.DispatchEvents();
            if (Keyboard.IsKeyPressed(Keyboard.Key.Escape)) window.Close();

            float deltaTime = clock.Restart().AsSeconds();
            
            // Обработка ввода
            float moveX = 0;
            if (Keyboard.IsKeyPressed(Keyboard.Key.A)) moveX -= 1;
            if (Keyboard.IsKeyPressed(Keyboard.Key.D)) moveX += 1;
            
            if (Keyboard.IsKeyPressed(Keyboard.Key.Space) && isGrounded)
            {
                velocity.Y = jumpForce;
                isGrounded = false;
            }

            // Физика
            velocity.X = moveX * movementSpeed;
            velocity.Y += gravity * deltaTime;

            spritePosition += velocity * deltaTime;

            // Коллизии
            isGrounded = false;
            FloatRect playerBounds = new FloatRect(spritePosition, new Vector2f(frameWidth, frameHeight));
            
            foreach (Platform platform in platforms)
            {
                FloatRect platformBounds = new FloatRect(platform.Shape.Position, platform.Shape.Size);
                
                if (playerBounds.Intersects(platformBounds))
                {
                    Vector2f overlap = new Vector2f(
                        Math.Min(playerBounds.Left + playerBounds.Width - platformBounds.Left, 
                              platformBounds.Left + platformBounds.Width - playerBounds.Left),
                        Math.Min(playerBounds.Top + playerBounds.Height - platformBounds.Top,
                              platformBounds.Top + platformBounds.Height - playerBounds.Top)
                    );

                    if (Math.Abs(overlap.X) < Math.Abs(overlap.Y))
                    {
                        spritePosition.X += overlap.X > 0 ? -overlap.X : overlap.X;
                        velocity.X = 0;
                    }
                    else
                    {
                        spritePosition.Y += overlap.Y > 0 ? -overlap.Y : overlap.Y;
                        velocity.Y = 0;
                        if (overlap.Y > 0) isGrounded = true;
                    }
                }
            }

            // Границы мира
            spritePosition.X = Math.Clamp(spritePosition.X, 0, worldSize.X - frameWidth);
            spritePosition.Y = Math.Clamp(spritePosition.Y, 0, worldSize.Y - frameHeight);

            // Анимация
            animationTimer += deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % framesPerRow;
            }

            // Направление спрайта
            if (moveX < 0) currentDirection = Direction.Left;
            else if (moveX > 0) currentDirection = Direction.Right;

            int frameY = currentDirection switch
            {
                Direction.Down => 0,
                Direction.Left => 1,
                Direction.Right => 2,
                Direction.Up => 3,
                _ => 0
            };

            sprite.TextureRect = new IntRect(
                currentFrame * frameWidth,
                frameY * frameHeight,
                frameWidth,
                frameHeight
            );

            // Камера
            Vector2f targetCameraPos = spritePosition + new Vector2f(frameWidth/2, frameHeight/2);
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
            sprite.Position = spritePosition;
            window.Clear(new Color(40, 40, 40));
            
            foreach (Platform platform in platforms)
                window.Draw(platform.Shape);
            
            window.Draw(sprite);
            window.Display();
        }
    }
}
