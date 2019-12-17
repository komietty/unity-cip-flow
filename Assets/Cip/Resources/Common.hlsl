#ifndef CIP_FLOW_COMMON
#define CIP_FLOW_COMMON

#define THREAD1D [numthreads(8, 1, 1)]
#define THREAD2D [numthreads(8, 8, 1)]

struct Particle {
	float3 pos;
	float3 vel;
};

struct Lattice {
	int type;
	float prs;
	float psi;
	float omg;
	float2 vel;
	float2 vxg;
	float2 vyg;
};

int width;
int height;
float threshold;
float deltaT;
float Re;
float obsH;
float obsW;
float obsX;
float obsY;
float prebObsX;
float prebObsY;

#endif