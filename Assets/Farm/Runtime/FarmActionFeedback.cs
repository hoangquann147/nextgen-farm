using NextGenFarm.Core;
using UnityEngine;

namespace NextGenFarm
{
    public sealed class FarmActionFeedback : MonoBehaviour
    {
        private FarmGridManager grid;
        private ParticleSystem particles;
        private Material material;
        public void Initialize(FarmGridManager farm)
        {
            grid=farm;var go=new GameObject("Tool feedback");go.transform.SetParent(transform,false);
            particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.playOnAwake=false;main.loop=false;main.startLifetime=.65f;main.startSpeed=2;main.startSize=.065f;main.gravityModifier=.6f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.7f;
            material=new Material(Shader.Find("HDRP/Unlit"));material.SetColor("_UnlitColor",new Color(.3f,.7f,1));particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
            grid.ActionApplied+=OnAction;
        }
        private void OnAction(Vector2Int cell,FarmAction action,int count)
        {
            Color color=action==FarmAction.Water?new Color(.2f,.7f,1):action==FarmAction.Harvest?new Color(1,.74f,.18f):new Color(.6f,.38f,.15f);
            material.SetColor("_UnlitColor",color*150);particles.transform.position=grid.CellCenter(cell.x,cell.y)+Vector3.up*.2f;
            particles.Emit(Mathf.Min(80,8*count));
        }
        private void OnDestroy(){if(grid!=null)grid.ActionApplied-=OnAction;if(material!=null)Destroy(material);}
    }
}
