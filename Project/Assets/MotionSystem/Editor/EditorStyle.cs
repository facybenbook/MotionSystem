using UnityEditor;
using UnityEngine;

namespace MotionSystem
{
    public class EditorStyle
    {
        public GUIStyle BlackBox, RedBox, GreenBox, LightBlueBox, BlueBox, GreyBox, TitleBarStyle, BigTitleColorStyle;
        public Color titleBarColor, bigTitleColor;
        const string m_styleName = "ShurikenModuleTitle";
        const string m_styleValue = "ShurikenValue";

        public void DrawTitleBar(string s)
        {
            GUILayout.Label(s, TitleBarStyle);
        }

        public void DrawBigTitleColor(string s)
        {
            GUILayout.Label(s, BigTitleColorStyle);
        }

        public void SetupStyle()
        {
            if (GreenBox == null)
            {
                GreenBox = new GUIStyle(GUI.skin.box);
                Color c = Color.green;
                c.a = Float.DotOne;
                GreenBox.normal.background = MakeTex(Int.Two, Int.Two, c);
            }

            if (BlueBox == null)
            {
                BlueBox = new GUIStyle(GUI.skin.box);
                Color c = Color.blue;
                c.a = Float.DotOne;
                BlueBox.normal.background = MakeTex(Int.Two, Int.Two, c);
            }

            if (LightBlueBox == null)
            {
                LightBlueBox = new GUIStyle(GUI.skin.box);
                Color c = new Color(Float.Zero, Float.DotNine, Float.DotNine);
                c.a = Float.DotOne;
                LightBlueBox.normal.background = MakeTex(Int.Two, Int.Two, c);
            }

            if (GreyBox == null)
            {
                GreyBox = new GUIStyle(GUI.skin.box);
                Color c = Color.grey;
                c.a = Float.DotOne;
                GreyBox.normal.background = MakeTex(Int.Two, Int.Two, c);
            }

            if (RedBox == null)
            {
                RedBox = new GUIStyle(GUI.skin.box);
                Color c = Color.red;
                c.a = Float.DotOne;
                RedBox.normal.background = MakeTex(Int.Two, Int.Two, c);
            }

            if (BlackBox == null)
            {
                BlackBox = new GUIStyle(GUI.skin.box);
                Color c = Color.black;
                c.a = Float.DotOne;
                BlackBox.normal.background = MakeTex(Int.Two, Int.Two, c);
            }

            if (TitleBarStyle == null)
            {
                const int maxHeight = 24;
                titleBarColor = new Color(Float.Zero, Float.Zero, Float.Zero, Float.DotSeven);
                GUIStyle skurikenModuleTitleStyle = m_styleName;
                TitleBarStyle = new GUIStyle(skurikenModuleTitleStyle);
                TitleBarStyle.contentOffset = new Vector2(Float.Five, -Float.Two);
                TitleBarStyle.normal.textColor = titleBarColor;
                TitleBarStyle.fixedHeight = maxHeight;
                TitleBarStyle.fontStyle = FontStyle.Bold;
            }

            if (BigTitleColorStyle == null)
            {
                if (EditorGUIUtility.isProSkin)
                    bigTitleColor = new Color(Float.One, Float.One, Float.One, Float.DotSeven);
                else
                    bigTitleColor = new Color(Float.Zero, Float.Zero, Float.Zero, Float.One);

                const int fontSize = 17;
                const int fixedHeight = 32;
                GUIStyle ss = m_styleValue;
                BigTitleColorStyle = new GUIStyle(ss);
                BigTitleColorStyle.contentOffset = new Vector2(Float.Five, Float.Zero);
                BigTitleColorStyle.normal.textColor = bigTitleColor;
                BigTitleColorStyle.fontSize = fontSize;
                BigTitleColorStyle.fixedHeight = fixedHeight;
                BigTitleColorStyle.fontStyle = FontStyle.Bold;
            }
        }

        public Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}