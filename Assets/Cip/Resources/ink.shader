Shader "Hidden/ink"
{
	SubShader
	{
	Tags{ "RenderType" = "Opaque" }
	LOD 100

	Pass
	{
		Blend SrcAlpha one

		CGPROGRAM
		#pragma target 5.0

		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"

		struct Particle
		{
			float3 pos;
			float3 vel;
		};

		struct v2f
		{
			float4 position : SV_POSITION;
			float4 color : COLOR;
		};

		StructuredBuffer<Particle> particleBuffer;

		v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
		{
			v2f o;
			float x = abs(particleBuffer[instance_id].vel.x);
			float y = abs(particleBuffer[instance_id].vel.y);
			float z = (x + y);
			o.color = float4(z * 1.2, z, z, 1.0);
			o.position = UnityObjectToClipPos(float4(particleBuffer[instance_id].pos, 1.0f));
			return o;
		};

		float4 frag(v2f i) : COLOR
		{
			return i.color;
		}
		ENDCG
		}
	}
}