// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using FishNet.Connection;
// using FishNet.Object;
// using FishNet.Object.Prediction;
// using FishNet.Transporting;

// public struct ShootData : IReplicateData
// {
//     public bool Shoot;

//     public ShootData(bool _shoot) {
//         Shoot = _shoot;
//         _tick = 0;
//     }
    
//     /* Everything below this is required for
//     * the interface. You do not need to implement
//     * Dispose, it is there if you want to clean up anything
//     * that may allocate when this structure is discarded. */
//     private uint _tick;
//     public void Dispose() { }
//     public uint GetTick() => _tick;
//     public void SetTick(uint value) => _tick = value;
// }

// public struct ShootReconcileData : IReconcileData
// {
//     public int Health;

//     public ShootReconcileData(int _health) {
//         Health = _health;
//         _tick = 0;
//     }
    
//     /* Everything below this is required for
//     * the interface. You do not need to implement
//     * Dispose, it is there if you want to clean up anything
//     * that may allocate when this structure is discarded. */
//     private uint _tick;
//     public void Dispose() { }
//     public uint GetTick() => _tick;
//     public void SetTick(uint value) => _tick = value;
// }

// public class PlayerHealthController : NetworkBehaviour
// {
//     private int health;
//     private bool shoot;

//     public override void OnStartClient()
//     {
//         base.OnStartClient();
//         //subscribes to time manager ontick and on post tick
//         base.TimeManager.OnTick += TimeManager_OnTick;
//         base.TimeManager.OnPostTick  += TimeManager_OnPostTick;
//     }

//     public override void OnStopNetwork()
//     {
//         base.OnStopNetwork();
//         if (base.TimeManager != null){
//             //unsubscribe from timemanager ticks
//             base.TimeManager.OnTick -= TimeManager_OnTick;
//             base.TimeManager.OnPostTick  -= TimeManager_OnPostTick ;
//         }
//     }

//     public void Update() {
//         if (base.IsOwner) {
//             if (Input.GetAxis("Shoot") != 0f) {
//                 shoot = true;
//             } else {
//                 shoot = false;
//             }
//         }
//     }

//     private void TimeManager_OnTick()
//     {
//         Shoot(BuildActions());
//     }

//     private void TimeManager_OnPostTick(){
//         if (base.IsServerInitialized)
//         {
//             Rigidbody rb = GetComponent<Rigidbody>();
//             ShootReconcileData rd = new ShootReconcileData(health);
//             Reconcile(rd);
//         }
//     }

//     private ShootData BuildActions() {
//         if (!base.IsOwner)
//         return default;

//         ShootData md = new ShootData(shoot);

//         return md;
//     }

//     [Replicate]
//     private void Shoot(ShootData shootData, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
//         if (shootData.Shoot)
//         {
//             RaycastHit hit;
//             Ray ray = new Ray(transform.Find("Head").position, transform.Find("Head").forward);
//             if (Physics.Raycast(ray, out hit, Mathf.Infinity)){
//                 if (hit.transform.gameObject.tag == "Player")
//                 {
//                     Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
//                     Debug.Log("Did Hit");
//                 }
//                 else
//                 {
//                     Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
//                     Debug.Log("Did not Hit");
//                 }
//             }
//         }
//     }

//     [Reconcile]
//     private void Reconcile(ShootReconcileData rd, Channel channel = Channel.Unreliable)
//     {
//         health = rd.Health;
//     }
// }
