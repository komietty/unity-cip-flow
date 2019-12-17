using UnityEngine;
using System.Runtime.InteropServices;

public class board : MonoBehaviour {
    struct Particle {
        public Vector3 pos;
        public Vector3 vel;
    }

    struct Lattice {
        public int type;
        public float prs;
        public float psi;
        public float omg;
        public Vector2 vel;
        public Vector2 vxg;
        public Vector2 vyg;
    }

    Shader shader;
    ComputeShader patricleCompShader;
    ComputeShader cipCompShader;
    ComputeShader prsCompShader;
    ComputeShader velCompShader;
    ComputeBuffer particlebuffer;
    ComputeBuffer latticebuffer;
    Material mat;

    // kernel number
    int updateKernel;
    int cipUpdateKernel;
    int prsUpdateKernel;
    int velUpdateKernel;

    // public valiables
    public int particleNum = 400000;
    public int latticeWidth = 1600;
    public int latticeHeight = 900;
    public float cameraPosZ = -800;
    public float Re = 2000;
    public float threshold = 100;
    public Vector2 brushSize;

    // obs
    public float obsW => brushSize.x;
    public float obsH => brushSize.y;
    float obsX = 0;
    float obsY = 0;
    float prebObsX = 0;
    float prebObsY = 0;

    // array length
    int latticeLen;

    void OnEnable() {
        latticeLen = latticeWidth * latticeHeight;
        mat = new Material(Shader.Find("Hidden/ink"));
        patricleCompShader = (ComputeShader)Resources.Load("particle");
        cipCompShader = (ComputeShader)Resources.Load("calc");
        prsCompShader = (ComputeShader)Resources.Load("pressure");
        velCompShader = (ComputeShader)Resources.Load("velocity");
        Particle[] pArr = new Particle[particleNum];
        Lattice[] lArr = new Lattice[latticeLen];

        for (int i = 0; i < particleNum; i++) {
            pArr[i].pos.x = Random.value * latticeWidth;
            pArr[i].pos.y = Random.value * latticeHeight;
            pArr[i].pos.z = 0;
            pArr[i].vel.x = 0;
            pArr[i].vel.y = 0;
            pArr[i].vel.z = 0;
        }

        for (int y = 0; y < latticeHeight; y++) {
            for (int x = 0; x < latticeWidth; x++) {
                int k = x + y * latticeWidth;
                int w = latticeWidth - 1;
                int h = latticeHeight - 1;

                // type data
                lArr[k].type = 0; // inside
                if (y == 0) lArr[k].type = 1; // bottom
                if (y == h) lArr[k].type = 2; // top
                if (x == 0) lArr[k].type = 3; // inlet
                if (x == w) lArr[k].type = 4; // outlet

                if (x == obsX && y > obsY && y < obsY + obsH) lArr[k].type = 5; // obs_left
                if (y == obsY && x > obsX && x < obsX + obsW) lArr[k].type = 6; // obs_bottom
                if (x == obsX + obsW && y > obsY && y < obsY + obsH) lArr[k].type = 7; // obs_right
                if (y == obsY + obsH && x > obsX && x < obsX + obsW) lArr[k].type = 8; // obs_top
                if (x > obsX && y > obsY && x < obsX + obsW && y < obsY + obsH) lArr[k].type = 9; // obs_inn

                if (x == obsX && y == obsY) lArr[k].type = 10; // obs_bottomleft
                if (x == obsX && y == obsY + obsH) lArr[k].type = 11; // obs_topleft
                if (x == obsX + obsW && y == obsY) lArr[k].type = 12; // obs_bottomright
                if (x == obsX + obsW && y == obsY + obsH) lArr[k].type = 13; // obs_topright

                // initial condition
                lArr[k].prs = 0;
                lArr[k].psi = 0;
                lArr[k].omg = 0;
                lArr[k].vxg = new Vector2(0, 0);
                lArr[k].vyg = new Vector2(0, 0);
                lArr[k].vel = new Vector2(0, 0);
            }
        }

        // init compute buffer
        particlebuffer = new ComputeBuffer(particleNum, Marshal.SizeOf(typeof(Particle)));
        latticebuffer = new ComputeBuffer(latticeLen, Marshal.SizeOf(typeof(Lattice)));
        particlebuffer.SetData(pArr);
        latticebuffer.SetData(lArr);

        // init kernel id 
        updateKernel = patricleCompShader.FindKernel("Update");
        cipUpdateKernel = cipCompShader.FindKernel("Update");
        prsUpdateKernel = prsCompShader.FindKernel("Update");
        velUpdateKernel = velCompShader.FindKernel("Update");

        mat.SetBuffer("particleBuffer", particlebuffer);

        BoxCollider box = gameObject.GetComponent<BoxCollider>();
        box.center = new Vector3(latticeWidth / 2, latticeHeight / 2, 0);
        box.size = new Vector3(latticeWidth, latticeHeight, 1);
    }

    void Update() {
        // update cip data
        cipCompShader.SetFloat("deltaT", Time.deltaTime);
        cipCompShader.SetFloat("Re", Re);
        cipCompShader.SetFloat("threshold", threshold);
        cipCompShader.SetInt("width", latticeWidth);
        cipCompShader.SetInt("height", latticeHeight);
        cipCompShader.SetFloat("obsX", obsX);
        cipCompShader.SetFloat("obsY", obsY);
        cipCompShader.SetFloat("prebObsX", prebObsX);
        cipCompShader.SetFloat("prebObsY", prebObsY);
        cipCompShader.SetFloat("obsW", obsW);
        cipCompShader.SetFloat("obsH", obsH);
        cipCompShader.SetBuffer(cipUpdateKernel, "LB", latticebuffer);
        cipCompShader.Dispatch(cipUpdateKernel, latticeWidth / 8, latticeHeight / 8, 1);

        // update prs data
        prsCompShader.SetFloat("deltaT", Time.deltaTime);
        prsCompShader.SetFloat("Re", Re);
        prsCompShader.SetInt("width", latticeWidth);
        prsCompShader.SetInt("height", latticeHeight);
        prsCompShader.SetBuffer(prsUpdateKernel, "LB", latticebuffer);
        for (int i = 0; i < 2; i++) {
            prsCompShader.Dispatch(prsUpdateKernel, latticeWidth / 8, latticeHeight / 8, 1);
        }

        // update vel data
        velCompShader.SetFloat("deltaT", Time.deltaTime);
        velCompShader.SetFloat("Re", Re);
        velCompShader.SetInt("width", latticeWidth);
        velCompShader.SetInt("height", latticeHeight);
        velCompShader.SetBuffer(velUpdateKernel, "LB", latticebuffer);
        velCompShader.Dispatch(velUpdateKernel, latticeWidth / 8, latticeHeight / 8, 1);

        // update particle data
        patricleCompShader.SetInt("width", latticeWidth);
        patricleCompShader.SetInt("height", latticeHeight);
        patricleCompShader.SetBuffer(updateKernel, "PB", particlebuffer);
        patricleCompShader.SetBuffer(updateKernel, "LB", latticebuffer);
        patricleCompShader.Dispatch(updateKernel, particleNum / 8 + 1, 1, 1);

        //get particle pos data
        Particle[] pArrData = new Particle[particleNum];
        particlebuffer.GetData(pArrData);
    }

    void OnDestroy() {
        particlebuffer.Release();
        latticebuffer.Release();
    }

    void OnRenderObject() {
        mat.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleNum);
    }

    void OnMouseDrag() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        Physics.Raycast(ray, out hit);
        prebObsX = obsX;
        prebObsY = obsY;

        if (hit.point.x > 0 && hit.point.x < latticeWidth && hit.point.y > 0 && hit.point.y < latticeHeight) {
            if (hit.collider.gameObject == gameObject) {
                obsX = hit.point.x;
                obsY = hit.point.y;
            }
        }
    }
}