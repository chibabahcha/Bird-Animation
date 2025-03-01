using SFML.Graphics;
using SFML.System;
using SFML.Window;

class Program
{
    enum Direction
    {
        Down,
        Left,
        Right,
        Up
    }

    static void Main()
    {
        RenderWindow window = new RenderWindow(new VideoMode(800, 600), "Eagle Animation");
        window.SetFramerateLimit(60);

        Texture texture = new Texture("assets\\eagle.png");
        Sprite sprite = new Sprite(texture);

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
        float movementSpeed = 150f;

        while (window.IsOpen)
        {
            window.DispatchEvents();
            if (Keyboard.IsKeyPressed(Keyboard.Key.Escape)) window.Close();

            float deltaTime = clock.Restart().AsSeconds();
            bool moved = false;

            if (Keyboard.IsKeyPressed(Keyboard.Key.W))
            {
                currentDirection = Direction.Up;
                spritePosition.Y -= movementSpeed * deltaTime;
                moved = true;
            }
            else if (Keyboard.IsKeyPressed(Keyboard.Key.S))
            {
                currentDirection = Direction.Down;
                spritePosition.Y += movementSpeed * deltaTime;
                moved = true;
            }

            if (Keyboard.IsKeyPressed(Keyboard.Key.A))
            {
                currentDirection = Direction.Left;
                spritePosition.X -= movementSpeed * deltaTime;
                moved = true;
            }
            else if (Keyboard.IsKeyPressed(Keyboard.Key.D))
            {
                currentDirection = Direction.Right;
                spritePosition.X += movementSpeed * deltaTime;
                moved = true;
            }

            animationTimer += deltaTime;
            if (animationTimer >= animationSpeed)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % framesPerRow;
            }

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


            spritePosition.X = Math.Clamp(spritePosition.X, 0, 800 - frameWidth);
            spritePosition.Y = Math.Clamp(spritePosition.Y, 0, 600 - frameHeight);

            sprite.Position = spritePosition;

            window.Clear(Color.Green);
            window.Draw(sprite);
            window.Display();
        }
    }
}