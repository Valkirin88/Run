Shader "Custom/Silhouette"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SilhouetteColor ("Silhouette Color", Color) = (1, 0, 0, 1)
        _SilhouetteThickness ("Silhouette Thickness", Range(0, 0.1)) = 0.05
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+1" } // Рендерим после прозрачных объектов

        // Основной проход (обычный рендеринг)
        Pass
        {
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i) : SV_Target { return fixed4(0, 0, 0, 0); } // Прозрачный
            ENDCG
        }

        // Проход для силуэта (рендерится, если объект закрыт)
        Pass
        {
            ZTest Greater // Рендерим только если объект позади других
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            fixed4 _SilhouetteColor;
            float _SilhouetteThickness;

            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.xyz += normalize(v.vertex.xyz) * _SilhouetteThickness; // Увеличиваем размер
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _SilhouetteColor; }
            ENDCG
        }
    }
}
