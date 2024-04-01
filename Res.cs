using Godot;
using Godot.Collections;
using System;

public partial class Res : RefCounted
{
    public const int Z_Background = -1;
    public const int Z_Floor = 0;
    public const int Z_Target = 1;
    public const int Z_Ground = 2;
    public const int Z_Swallow = 20;

    public const float Scale_Swallow_f = 0.7f;
    public static readonly Vector2 Scale_Swallow = new(Scale_Swallow_f, Scale_Swallow_f);
    public static readonly Vector2 Scale_Normal = new(1f, 1f);

    private static readonly Dictionary<string, ImageTexture> textureMap = new();
    public static ImageTexture Type_Target_Vailed;

    public static void Init()
    {
        //InitElementTexture();
    }

    public static void InitElementTexture()
    {
        var width = ElementNode.Element_Size;
        var borderWidth = ElementNode.Element_Border_Size;
        foreach (var type in Enum.GetValues<Type>())
        {
            Image img = Image.Create(width, width, false, Image.Format.Rgba8);
            switch (type)
            {
                case Type.Player:
                    img.Fill(Colors.Red);
                    //for (int x = 0; x < width; x++)
                    //{
                    //    for (int y = 0; y < width; y++)
                    //    {
                    //        if ((x - width / 2) * (x - width / 2) + (y - width / 2) * (y - width / 2) > width * width / 2.5)
                    //        {
                    //            img.SetPixel(x, y, Colors.Transparent);
                    //        }
                    //    }
                    //}
                    break;
                case Type.Wall:
                    img.Fill(Colors.Black);
                    break;
                case Type.Box:
                    {
                        img.Fill(Colors.White);
                        var color = new Color(0.8f, 0.8f, 0.8f, 1f);
                        img.FillRect(new Rect2I(0, 0, width, borderWidth), color);
                        img.FillRect(new Rect2I(0, 0, borderWidth, width), color);
                        img.FillRect(new Rect2I(width - borderWidth, 0, width, width), color);
                        img.FillRect(new Rect2I(0, width - borderWidth, width, width), color);
                        break;
                    }
                case Type.Target:
                    {

                        var color = new Color(0f, 0f, 1f, 0.3f);
                        img.FillRect(new Rect2I(0, 0, width, borderWidth), color);
                        img.FillRect(new Rect2I(0, 0, borderWidth, width), color);
                        img.FillRect(new Rect2I(width - borderWidth, 0, width, width), color);
                        img.FillRect(new Rect2I(0, width - borderWidth, width, width), color);
                        break;
                    }
                default:
                    img.Fill(Colors.LightBlue);
                    break;
            }
            textureMap[type.ToString()] = ImageTexture.CreateFromImage(img);
            img.Dispose();
        }


        foreach (var dir in Enum.GetValues<DIR>())
        {
            Image img = Image.Create(width, width, false, Image.Format.Rgba8);
            switch (dir)
            {
                case DIR.UP:
                    img.FillRect(new Rect2I(0, 0, width, borderWidth * 2), Colors.White);
                    break;
                case DIR.LEFT:
                    img.FillRect(new Rect2I(0, 0, borderWidth * 2, width), Colors.White);
                    break;
                case DIR.RIGHT:
                    img.FillRect(new Rect2I(width - borderWidth * 2, 0, width, width), Colors.White);
                    break;
                case DIR.DOWN:
                    img.FillRect(new Rect2I(0, width - borderWidth * 2, width, width), Colors.White);
                    break;
            }
            textureMap[dir.ToString()] = ImageTexture.CreateFromImage(img);
            img.Dispose();
        }

        {

            Image img = Image.Create(width, width, false, Image.Format.Rgba8);
            img.Fill(Colors.LightBlue);
            Type_Target_Vailed = ImageTexture.CreateFromImage(img);
            img.Dispose();
        }
    }

    public static ImageTexture GetImageTexture(Type type)
    {
        return textureMap[type.ToString()];
    }
    public static ImageTexture GetImageTexture(DIR dir)
    {
        return textureMap[dir.ToString()];
    }
}
