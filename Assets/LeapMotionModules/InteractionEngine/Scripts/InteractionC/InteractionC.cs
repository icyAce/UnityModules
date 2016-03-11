﻿using UnityEngine;
using System;
using System.Runtime.InteropServices;
using LeapInternal;

namespace InteractionEngine.CApi {

  public enum eLeapIERS : uint {
    eLeapIERS_Success,
    eLeapIERS_InvalidArgument,
    eLeapIERS_ReferencesRemain,
    eLeapIERS_NotEnabled,
    eLeapIERS_UnknownError = 0x10000000
  }

  public enum eLeapIEShapeType : uint {
    eLeapIEShape_Sphere,
    eLeapIEShape_OBB,
    eLeapIEShape_Convex,
    eLeapIEShape_Compound,
    eLeapIEShape_ForceTo32Bits = 0x10000000
  }

  public enum eLeapIEClassification : uint {
    eLeapIEClassification_None,
    eLeapIEClassification_Push,
    eLeapIEClassification_Grab,
    eLeapIEClassification_ForceTo32Bits = 0x10000000
  }

  public enum eLeapIEDebugFlags {
    eLeapIEDebugFlags_None,
    eLeapIEDebugFlags_LinesInternal = 0x01,
    eLeapIEDebugFlags_RecordToFile = 0x02,
    eLeapIEDebugFlags_RecordToAnalytics = 0x03,
    eLeapIEDebugFlags_ForceTo32Bits = 0x10000000
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_QUATERNION {
    public float w;
    public float x;
    public float y;
    public float z;

    public LEAP_QUATERNION(Quaternion unity) {
      w = unity.w;
      x = unity.x;
      y = unity.y;
      z = unity.z;
    }

    public Quaternion ToUnityRotation() {
      return new Quaternion(x, y, z, w);
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_COLOR {
    float r;
    float g;
    float b;
    float a;

    public LEAP_COLOR(Color color) {
      r = color.r;
      g = color.g;
      b = color.b;
      a = color.a;
    }

    public Color ToUnityColor() {
      return new Color(r, g, b, a);
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SCENE {
    public IntPtr pData; //LeapIESceneData*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_TRANSFORM {
    public LEAP_VECTOR position;
    public LEAP_QUATERNION rotation;
    public float wallTime;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION {
    public eLeapIEShapeType type;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SPHERE_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public float radius;

    public LEAP_IE_SPHERE_DESCRIPTION(SphereCollider sphereCollider) {
      shape.type = eLeapIEShapeType.eLeapIEShape_Sphere;
      radius = sphereCollider.transform.lossyScale.x * sphereCollider.radius;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_OBB_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public LEAP_VECTOR extents;

    public LEAP_IE_OBB_DESCRIPTION(BoxCollider boxCollider) {
      shape.type = eLeapIEShapeType.eLeapIEShape_OBB;
      extents = new LEAP_VECTOR(boxCollider.transform.TransformVector(boxCollider.size * 0.5f));
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public UInt32 nVerticies;
    public IntPtr pVertices; //LEAP_VECTOR*
    public float radius;

    public LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(SphereCollider sphereCollider) {
      shape.type = eLeapIEShapeType.eLeapIEShape_Convex;
      nVerticies = 1;

      pVertices = StructAllocator.AllocateArray<LEAP_VECTOR>(1);
      Vector3 scaledCenter = sphereCollider.transform.TransformVector(sphereCollider.center);
      StructMarshal<LEAP_VECTOR>.CopyIntoArray(pVertices, new LEAP_VECTOR(scaledCenter), 0);

      radius = sphereCollider.transform.lossyScale.x * sphereCollider.radius;
    }

    public LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(BoxCollider boxCollider) {
      shape.type = eLeapIEShapeType.eLeapIEShape_Convex;

      pVertices = StructAllocator.AllocateArray<LEAP_VECTOR>(8);

      nVerticies = 0;
      Vector3 scaledCenter = boxCollider.transform.TransformVector(boxCollider.center);
      for (int dx = -1; dx <= 1; dx += 2) {
        for (int dy = -1; dy <= 1; dy += 2) {
          for (int dz = 1; dz <= 1; dz += 2) {
            Vector3 corner = Vector3.Scale(boxCollider.size, new Vector3(dx, dy, dz)) * 0.5f;
            corner += scaledCenter;
            StructMarshal<LEAP_VECTOR>.CopyIntoArray(pVertices, new LEAP_VECTOR(corner), (int)nVerticies++);
          }
        }
      }

      radius = 0.0f;
    }

    public LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(CapsuleCollider capsuleCollider) {
      shape.type = eLeapIEShapeType.eLeapIEShape_Convex;

      nVerticies = 2;

      pVertices = StructAllocator.AllocateArray<LEAP_VECTOR>(2);
      Vector3 center = capsuleCollider.transform.TransformVector(capsuleCollider.center);
      float scaledHeight = capsuleCollider.transform.lossyScale.x * capsuleCollider.height;
      Vector3 axis;
      switch (capsuleCollider.direction) {
        case 0:
          axis = Vector3.right;
          break;
        case 1:
          axis = Vector3.up;
          break;
        case 2:
          axis = Vector3.forward;
          break;
        default:
          throw new InvalidOperationException("Unexpected capsule direction " + capsuleCollider.direction);
      }

      Vector3 p0 = center + axis * scaledHeight * 0.5f;
      Vector3 p1 = center - axis * scaledHeight * 0.5f;

      StructMarshal<LEAP_VECTOR>.CopyIntoArray(pVertices, new LEAP_VECTOR(p0), 0);
      StructMarshal<LEAP_VECTOR>.CopyIntoArray(pVertices, new LEAP_VECTOR(p1), 1);

      radius = capsuleCollider.transform.lossyScale.x * capsuleCollider.radius;
    }

    public LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(MeshCollider meshCollider) {
      shape.type = eLeapIEShapeType.eLeapIEShape_Convex;
      Mesh convexMesh = meshCollider.sharedMesh;
      Vector3[] verts = convexMesh.vertices;

      nVerticies = (UInt32)convexMesh.vertexCount;
      pVertices = StructAllocator.AllocateArray<LEAP_VECTOR>(verts.Length);
      for (int i = 0; i < verts.Length; i++) {
        LEAP_VECTOR v = new LEAP_VECTOR(meshCollider.transform.TransformVector(verts[i]));
        StructMarshal<LEAP_VECTOR>.CopyIntoArray(pVertices, v, i);
      }

      radius = 0.0f;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_COMPOUND_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public UInt32 nShapes;
    public IntPtr pShapes; //LEAP_IE_SHAPE_DESCRIPTION**
    public IntPtr pTransforms; //LEAP_IE_TRANSFORM* 

    public LEAP_IE_COMPOUND_DESCRIPTION(GameObject obj) {
      shape.type = eLeapIEShapeType.eLeapIEShape_Compound;

      Collider[] colliders = obj.GetComponentsInChildren<Collider>();
      nShapes = (uint)colliders.Length;

      pShapes = StructAllocator.AllocateArray<IntPtr>(colliders.Length);
      pTransforms = StructAllocator.AllocateArray<LEAP_IE_TRANSFORM>(colliders.Length);
      for (int i = 0; i < colliders.Length; i++) {
        LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION desc;

        Collider collider = colliders[i];
        if (collider is SphereCollider) {
          desc = new LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(collider as SphereCollider);
        } else if (collider is BoxCollider) {
          desc = new LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(collider as BoxCollider);
        } else if (collider is CapsuleCollider) {
          desc = new LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(collider as CapsuleCollider);
        } else if (collider is MeshCollider) {
          desc = new LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION(collider as MeshCollider);
        } else {
          throw new InvalidOperationException("Unsupported collider type " + collider.GetType());
        }

        IntPtr descPtr = StructAllocator.AllocateStruct(desc);
        StructMarshal<IntPtr>.CopyIntoArray(pShapes, descPtr, i);
      }
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION_HANDLE {
    public UInt32 handle;
    public IntPtr pDEBUG; // LeapIEShapeDescriptionData*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_INSTANCE_HANDLE {
    public UInt32 handle;
    public IntPtr pDEBUG; // LeapIEShapeInstanceData* 
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_CLASSIFICATION {
    public eLeapIEClassification classification;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_DEBUG_LINE {
    LEAP_VECTOR start;
    LEAP_VECTOR end;
    LEAP_COLOR color;
    float duration;
    int depthTest;

    public void Draw() {
      UnityEngine.Debug.DrawLine(start.ToUnityVector(),
                                 end.ToUnityVector(),
                                 color.ToUnityColor(),
                                 duration,
                                 depthTest != 0);
    }
  }

  public class InteractionC {
    public const string DLL_NAME = "LeapInteractionEngine";

    public static void HandleReturnStatus(eLeapIERS) {

    }

    public static void Log(string message) {

    }

    /*** Create Scene ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateScene")]
    private static extern eLeapIERS LeapIECreateScene(ref LEAP_IE_SCENE scene);

    public static void CreateScene(ref LEAP_IE_SCENE scene) {
      Log("Create Scene");
      var rs = LeapIECreateScene(ref scene);
      HandleReturnStatus(rs);
    }

    /*** Destroy Scene ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyScene")]
    private static extern eLeapIERS LeapIEDestroyScene(ref LEAP_IE_SCENE scene);

    public static void DestroyScene(ref LEAP_IE_SCENE scene) {
      Log("Destroy Scene");
      var rs = LeapIEDestroyScene(ref scene);
      HandleReturnStatus(rs);
    }

    /*** Add Shape Description ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEAddShapeDescription")]
    private static extern eLeapIERS LeapIEAddShapeDescription(ref LEAP_IE_SCENE scene,
                                                                  IntPtr pDescription,
                                                              out LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    public static void AddShapeDescription(ref LEAP_IE_SCENE scene,
                                               IntPtr pDescription,
                                           out LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle) {
      Log("Add Shape Description");
      var rs = LeapIEAddShapeDescription(ref scene, pDescription, out handle);
      HandleReturnStatus(rs);
    }

    /*** Remove Shape Description ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIERemoveShapeDescription")]
    private static extern eLeapIERS LeapIERemoveShapeDescription(ref LEAP_IE_SCENE scene,
                                                                 ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    public static void RemoveShapeDescription(ref LEAP_IE_SCENE scene,
                                              ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle) {
      Log("Remove Shape Description");
      var rs = LeapIERemoveShapeDescription(ref scene, ref handle);
      HandleReturnStatus(rs);
    }

    /*** Create Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateShape")]
    private static extern eLeapIERS LeapIECreateShape(ref LEAP_IE_SCENE scene,
                                                      ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle,
                                                      ref LEAP_IE_TRANSFORM transform,
                                                      out LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static void CreateShape(ref LEAP_IE_SCENE scene,
                                   ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle,
                                   ref LEAP_IE_TRANSFORM transform,
                                   out LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Log("Create Shape");
      var rs = LeapIECreateShape(ref scene, ref handle, ref transform, out instance);
      HandleReturnStatus(rs);
    }

    /*** Destroy Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyShape")]
    private static extern eLeapIERS LeapIEDestroyShape(ref LEAP_IE_SCENE scene,
                                                       ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static void DestroyShape(ref LEAP_IE_SCENE scene,
                                    ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Log("Destroy Shape");
      var rs = LeapIEDestroyShape(ref scene, ref instance);
      HandleReturnStatus(rs);
    }

    /*** Update Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateShape")]
    private static extern eLeapIERS LeapIEUpdateShape(ref LEAP_IE_SCENE scene,
                                                      ref LEAP_IE_TRANSFORM transform,
                                                      ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static void UpdateShape(ref LEAP_IE_SCENE scene,
                                   ref LEAP_IE_TRANSFORM transform,
                                   ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Log("Update Shape");
      var rs = LeapIEUpdateShape(ref scene, ref transform, ref instance);
      HandleReturnStatus(rs);
    }

    /*** Update Hands ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateHands")]
    private static extern eLeapIERS LeapIEUpdateHands(ref LEAP_IE_SCENE scene,
                                                          UInt32 nHands,
                                                          IntPtr pHands);

    public static void UpdateHands(ref LEAP_IE_SCENE scene,
                                       UInt32 nHands,
                                       IntPtr pHands) {
      Log("Update Hands");
      var rs = LeapIEUpdateHands(ref scene, nHands, pHands);
      HandleReturnStatus(rs);
    }

    public static void UpdateHands(ref LEAP_IE_SCENE scene,
                                       Leap.Frame frame) {
      IntPtr handArray = HandArrayBuilder.CreateHandArray(frame);
      UpdateHands(ref scene, (uint)frame.Hands.Count, handArray);
      StructAllocator.CleanupAllocations();
    }

    /*** Update Controller ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateController")]
    private static extern eLeapIERS LeapIEUpdateController(ref LEAP_IE_SCENE scene,
                                                           ref LEAP_IE_TRANSFORM controllerTransform);

    public static void UpdateController(ref LEAP_IE_SCENE scene,
                                        ref LEAP_IE_TRANSFORM controllerTransform) {
      Log("Update Controller");
      var rs = LeapIEUpdateController(ref scene, ref controllerTransform);
      HandleReturnStatus(rs);
    }

    /*** Get Classification ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetClassification")]
    private static extern eLeapIERS LeapIEGetClassification(ref LEAP_IE_SCENE scene,
                                                            ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance,
                                                            out LEAP_IE_SHAPE_CLASSIFICATION classification);

    public static void GetClassification(ref LEAP_IE_SCENE scene,
                                         ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance,
                                         out LEAP_IE_SHAPE_CLASSIFICATION classification) {
      Log("Get Classification");
      var rs = LeapIEGetClassification(ref scene, ref instance, out classification);
      HandleReturnStatus(rs);
    }

    /*** Enable Debug Visualization ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEEnableDebugFlags")]
    private static extern eLeapIERS LeapIEEnableDebugFlags(ref LEAP_IE_SCENE scene,
                                                               UInt32 flags);

    public static void EnableDebugFlags(ref LEAP_IE_SCENE scene,
                                            UInt32 flags) {
      Log("Enable Debug Flags");
      var rs = LeapIEEnableDebugFlags(ref scene, flags);
      HandleReturnStatus(rs);
    }

    /*** Get Debug Lines ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetDebugLines")]
    private static extern eLeapIERS LeapIEGetDebugLines(ref LEAP_IE_SCENE scene,
                                                        out UInt32 nLines,
                                                        out IntPtr ppLineBuffer);

    public static void GetDebugLines(ref LEAP_IE_SCENE scene,
                                     out UInt32 nLines,
                                     out IntPtr ppLineBuffer) {
      Log("Get Debug Lines");
      var rs = LeapIEGetDebugLines(ref scene, out nLines, out ppLineBuffer);
      HandleReturnStatus(rs);
    }

    public static void DrawDebugLines(ref LEAP_IE_SCENE scene) {
      UInt32 lines;
      IntPtr arrayPtr;
      GetDebugLines(ref scene, out lines, out arrayPtr);

      for (int i = 0; i < lines; i++) {
        IntPtr linePtr = StructMarshal<IntPtr>.ArrayElementToStruct(arrayPtr, i);
        LEAP_IE_DEBUG_LINE line = StructMarshal<LEAP_IE_DEBUG_LINE>.PtrToStruct(linePtr);
        line.Draw();
      }
    }

  }
}
