using Godot;

// Draws a cartoon character with code (body, head, face) and animates it while it walks.
// Used by both the Player and the Enemy: only the exported colors and options differ.
//
// Coordinates: (0, 0) is between the feet. The body goes up (negative Y), about 50 px tall.
// This matters for depth sorting: Godot sorts characters by the Y of their feet.
public partial class CharacterVisual : Node2D
{
    [Export] public Color SkinColor = new(1f, 0.82f, 0.66f);
    [Export] public Color HairColor = new(0.36f, 0.22f, 0.12f);
    [Export] public Color ShirtColor = new(0.25f, 0.55f, 1f);
    [Export] public Color PantsColor = new(0.22f, 0.24f, 0.38f);
    [Export] public Color ShoeColor = new(0.2f, 0.13f, 0.1f);
    [Export] public Color EyeColor = new(0.15f, 0.2f, 0.35f);
    [Export] public bool HasHair = true;
    // Zombie pose: arms stretched forward, yellowish eyes, open mouth.
    [Export] public bool ZombieStyle = false;
    // How fast the legs swing compared to the movement speed.
    [Export] public float StepFrequency = 0.06f;

    private CharacterBody2D _body;

    // Position in the walk cycle, in radians: Sin(_walkPhase) swings the legs back and forth.
    private float _walkPhase;
    // 0 = standing still, 1 = walking. Smoothed so the legs don't snap when stopping.
    private float _moveAmount;
    // 1 = facing right, -1 = facing left.
    private int _facing = 1;
    // The direction the eyes look at (normalized).
    private Vector2 _look = Vector2.Down;
    // Counts down to the next blink; the eyes are closed while it is below 0.
    private float _blinkTimer;

    public override void _Ready()
    {
        // The visual is a child of the body: it reads the body's Velocity to animate.
        _body = GetParent<CharacterBody2D>();
        _blinkTimer = (float)GD.RandRange(1.0, 4.0);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        Vector2 velocity = _body.Velocity;
        float speed = velocity.Length();
        bool isMoving = speed > 5f;

        if (isMoving)
        {
            _walkPhase += speed * StepFrequency * dt;
            _look = velocity / speed;
            if (Mathf.Abs(velocity.X) > 5f)
                _facing = velocity.X > 0 ? 1 : -1;
        }
        _moveAmount = Mathf.MoveToward(_moveAmount, isMoving ? 1f : 0f, dt * 6f);

        _blinkTimer -= dt;
        if (_blinkTimer < -0.12f)
            _blinkTimer = (float)GD.RandRange(2.0, 5.0);

        // _Draw() is cached by Godot. We ask for a new drawing every frame because we animate.
        QueueRedraw();
    }

    public override void _Draw()
    {
        float swing = Mathf.Sin(_walkPhase) * _moveAmount;
        // The upper body goes slightly up and down twice per step.
        float bob = -Mathf.Abs(Mathf.Cos(_walkPhase)) * 2f * _moveAmount;
        Vector2 up = new(0, bob);

        // Shadow on the ground (drawn before mirroring: it is symmetric anyway).
        this.DrawEllipse(new Vector2(0, 1), new Vector2(13, 5), new Color(0, 0, 0, 0.25f));

        // Mirror everything horizontally when facing left: "forward" is always +X below.
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_facing, 1));

        DrawLegs(swing);
        if (!ZombieStyle)
            DrawArm(new Vector2(-7, -27) + up, new Vector2(-9 - swing * 4, -16) + up);   // back arm

        // Torso: a thick rounded line, with a belt.
        this.DrawLimb(new Vector2(0, -27) + up, new Vector2(0, -16) + up, 15, ShirtColor);
        DrawLine(new Vector2(-7.5f, -12.5f) + up, new Vector2(7.5f, -12.5f) + up, PantsColor.Darkened(0.3f), 2.5f);

        if (ZombieStyle)
        {
            // Both arms stretched forward.
            DrawArm(new Vector2(-4, -27) + up, new Vector2(13, -26 + swing) + up);
            DrawArm(new Vector2(5, -26) + up, new Vector2(16, -23 - swing) + up);
            // Torn shirt bottom.
            DrawColoredPolygon(new[]
            {
                new Vector2(-7, -11) + up, new Vector2(-4, -7) + up, new Vector2(-1, -11) + up,
                new Vector2(2, -7) + up, new Vector2(5, -11) + up,
            }, ShirtColor);
        }
        else
        {
            DrawArm(new Vector2(7, -27) + up, new Vector2(9 + swing * 4, -16) + up);   // front arm
        }

        DrawHead(new Vector2(0, -40) + up);

        // Reset the transform so nothing else is mirrored by accident.
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void DrawLegs(float swing)
    {
        // One leg forward while the other goes back, and the moving leg lifts a little.
        var backFoot = new Vector2(-4.5f - swing * 5, -2 - Mathf.Max(0, swing) * 2);
        var frontFoot = new Vector2(4.5f + swing * 5, -2 - Mathf.Max(0, -swing) * 2);

        this.DrawLimb(new Vector2(-4, -12), backFoot, 6, PantsColor.Darkened(0.15f));
        this.DrawOutlinedCircle(backFoot + new Vector2(1.5f, 1), 3.2f, ShoeColor, 1.2f);

        this.DrawLimb(new Vector2(4, -12), frontFoot, 6, PantsColor);
        this.DrawOutlinedCircle(frontFoot + new Vector2(1.5f, 1), 3.2f, ShoeColor, 1.2f);
    }

    private void DrawArm(Vector2 shoulder, Vector2 hand)
    {
        this.DrawLimb(shoulder, hand, 5, ShirtColor.Darkened(0.1f));
        this.DrawOutlinedCircle(hand, 2.8f, SkinColor, 1.2f);
    }

    private void DrawHead(Vector2 center)
    {
        if (HasHair)
            this.DrawOutlinedCircle(center + new Vector2(0, -1.5f), 10.5f, HairColor);
        else
            this.DrawOutlinedCircle(center, 10f, SkinColor);

        // Face circle, slightly lower than the hair so the hair shows on top.
        DrawCircle(center + new Vector2(0.5f, 1.5f), 9f, SkinColor);
        if (HasHair)
        {
            // A fringe falling on the forehead.
            DrawColoredPolygon(new[]
            {
                center + new Vector2(-8, -4), center + new Vector2(8, -6),
                center + new Vector2(6, -1), center + new Vector2(1, -3), center + new Vector2(-4, 0),
            }, HairColor);
        }

        // Eyes look where the character walks. The mirror transform already flips X,
        // so we flip the look direction back to keep it correct.
        var look = new Vector2(_look.X * _facing, _look.Y) * 1.3f;
        var leftEye = center + new Vector2(-3, 1.5f);
        var rightEye = center + new Vector2(4.5f, 1.5f);
        bool blinking = _blinkTimer < 0;

        foreach (Vector2 eye in new[] { leftEye, rightEye })
        {
            if (blinking)
            {
                DrawLine(eye + new Vector2(-2, 0), eye + new Vector2(2, 0), DrawUtils.Outline, 1.3f);
                continue;
            }
            DrawCircle(eye, 2.6f, ZombieStyle ? new Color(1f, 0.95f, 0.6f) : Colors.White);
            DrawCircle(eye + look, 1.5f, EyeColor);
        }

        if (ZombieStyle)
        {
            // Open mouth and a darker patch of skin.
            this.DrawEllipse(center + new Vector2(1, 6), new Vector2(2.5f, 2f), new Color(0.3f, 0.05f, 0.08f));
            DrawCircle(center + new Vector2(-5, -3), 2f, SkinColor.Darkened(0.25f));
        }
        else
        {
            // Smile: the bottom half of a circle (angles go clockwise, 0 = right, PI/2 = down).
            DrawArc(center + new Vector2(1, 4.5f), 2.6f, 0.2f, Mathf.Pi - 0.2f, 8, DrawUtils.Outline, 1.2f);
            // Blushing cheeks.
            DrawCircle(center + new Vector2(-6, 4.5f), 1.6f, new Color(1f, 0.45f, 0.45f, 0.4f));
            DrawCircle(center + new Vector2(7.5f, 4.5f), 1.6f, new Color(1f, 0.45f, 0.45f, 0.4f));
        }
    }
}
