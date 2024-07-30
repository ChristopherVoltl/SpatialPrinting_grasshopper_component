using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using SMT;
using static SMT.SMTUtilities;
using Rhino;
using System.Linq;

namespace SpatialPrinting
{
    public class SpatialPrintingComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        static SuperMatterToolsPlugin smtPlugin => SuperMatterToolsPlugin.Instance;

        public SpatialPrintingComponent() : base("SpatialPrintingComponent", "SPC", "Spatial printing sorting component", "FGAM", "SpatialPrinting")
        {

        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("lineObjs ", "pO", "an array of lineObject", GH_ParamAccess.item);
            pManager.AddPlaneParameter("pathPlanes ", "pP", " an array of Plane", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Reverse", "R", "Reversed string", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 1. Declare placeholder variables and assign initial invalid data.
            //    This way, if the input parameters fail to supply valid data, we know when to abort.
            Rhino.Geometry.Line lineObjs = Rhino.Geometry.Line.Unset;
            Rhino.Geometry.Plane[] pathPlanes = null;

            //get the operation UI!
            int progIndex = smtPlugin.UIData.ProgramIndex;
            int opIndex = smtPlugin.UIData.OperationIndex;
            if (progIndex > -1 && opIndex > -1)
            {
                OperationUI opUI = smtPlugin.UIData.TreeRootUI.WC.ChildNodes[progIndex].ChildNodes[opIndex];
                if (opUI != null && lineObjs != Rhino.Geometry.Line.Unset)
                {

                    opUI.DivStyle = DivisionStyle.PointData;
                    opUI.FeedMode = FeedMapping.PointData;
                    opUI.ZOrientationStyle = ZOrientStyle.PointData;
                    opUI.YOrientationStyle = YOrientStyle.PointData;
                    opUI.LIStyle = InOutStyle.Inactive;
                    opUI.LOStyle = InOutStyle.Inactive;
                    //opUI.ApproxDist = 0.0f;
                    opUI.PTP_Traverse = true;
                    ActionState gripAct = opUI.SuperOperationRef.GetActionState("Gripper");
                    SuperActionUI actionUI = opUI.ActionControls["Gripper"];
                    actionUI.ActivationMode = ActivationStyle.PointData;
                    SuperEvent grip = new SuperEvent(gripAct, 0.0, EventType.Activate, true);
                    SuperEvent unGrip = new SuperEvent(gripAct, 0.0, EventType.Deactivate, true);


                    //given an array of ordered and oriented planes for each final  block location
                    //build paths
                    //fixed pickup location
                    Point3d pickPt = new Point3d(1375, -1892, 47);
                    Vector3d pickZ = new Vector3d(0, 1, 0);
                    Vector3d pickX = new Vector3d(1, 0, 0);
                    Vector3d pickY = new Vector3d(0, 0, -1);
                    Plane pickup = new Plane(pickPt, pickX, pickY);
                    Plane approachPick = new Plane(pickup);//move along -Z of tool
                    approachPick.Translate(pickZ * -50);
                    Plane safe0 = approachPick;//move up from approach  on World Z
                    safe0.Translate(Vector3d.ZAxis * 300);
                    Plane pickLift = pickup;//move up on WorldZ
                    pickLift.Translate(Vector3d.ZAxis * 300);

                    SuperShape[] shapes = new SuperShape[pathPlanes.Length];
                    SMTPart[] parts = new SMTPart[lineObjs.Length];
                    Transform[] initPartXform = new Transform[lineObjs.Length];

                    Point3d midPt = new Point3d(1236, 0, 1570);
                    Vector3d midZ = new Vector3d(0, 0, -1);
                    Vector3d midX = new Vector3d(-1, 0, 0);
                    Vector3d midY = new Vector3d(0, 1, 0);
                    Plane midPl = new Plane(midPt, midX, midY);
                    //we can use action states or events. try events first
                    for (int i = 0; i < pathPlanes.Length; i++)
                    {
                        SMTPData[] pData = new SMTPData[12];
                        //create the pickup data
                        pData[0] = new SMTPData(0, 0, 0, MoveType.Joint, safe0, 1.0f);
                        pData[1] = new SMTPData(1, 1, 1, MoveType.Lin, approachPick, 1.0f);
                        pData[2] = new SMTPData(2, 2, 2, MoveType.Lin, pickup, grip, 1.0f);
                        pData[3] = new SMTPData(3, 3, 3, MoveType.Lin, pickLift, 1.0f);
                        pData[4] = new SMTPData(4, 4, 4, MoveType.Joint, safe0, 1.0f);

                        pData[5] = new SMTPData(5, 5, 5, MoveType.Joint, midPl, 1.0f);
                        //for each point, create a safe approach, approach place, place. Then cycle back through them
                        Plane place = pathPlanes[i];
                        Plane approachPlace = place;//World Z from Place
                        approachPlace.Translate(Vector3d.ZAxis * 100);
                        Plane safe1 = approachPlace;
                        safe1.Translate(Vector3d.ZAxis * 200);

                        //create the place data
                        pData[6] = new SMTPData(6, 6, 6, MoveType.Joint, safe1, 1.0f);
                        pData[7] = new SMTPData(7, 7, 7, MoveType.Lin, approachPlace, 1.0f);
                        pData[8] = new SMTPData(8, 8, 8, MoveType.Lin, place, unGrip, 1.0f);
                        pData[9] = new SMTPData(9, 9, 9, MoveType.Lin, approachPlace, 1.0f);
                        pData[10] = new SMTPData(10, 10, 10, MoveType.Joint, safe1, 1.0f);
                        pData[11] = new SMTPData(11, 11, 11, MoveType.Joint, midPl, 1.0f);

                        //finished with path
                        Guid guid = Guid.NewGuid();
                        smtPlugin.UserData[guid] = pData;

                        shapes[i] = SuperShape.SuperShapeFactory(guid, null, DivisionStyle.PointData, ZOrientStyle.PointData, VectorStyle.ByParam, YOrientStyle.PointData, false, 0.0, Plane.WorldXY);
                        //smtPlugin.UserGeometry[guid] = partObjs[i].ExtrusionGeometry;
                        Transform xForm = Transform.PlaneToPlane(place, pickup);
                        initPartXform[i] = xForm;

                    }
                    //Parallel.For(0, shapes.Length, i =>
                    for (int i = 0; i < shapes.Length; i++)
                    {
                        RhinoDoc.ActiveDoc.Objects.Hide(lineObjs[i], true);
                        Guid newPartId = RhinoDoc.ActiveDoc.Objects.Transform(lineObjs[i].Id, initPartXform[i], false);
                        SMTRhObj part = new SMTRhObj(newPartId);
                        shapes[i].Part = part;
                    }//);
                    if (shapes.Length > 0)
                    {
                        var spbs = opUI.ReadFromGH(shapes);
                        if (spbs != null)
                        {
                            spbs.Last().IsSelected = true;
                            opUI.IsSelected = true;
                            //spbs.Last().IsSelected = true;
                        }
                    }
                }
                else
                    RhinoApp.WriteLine("You must select an Operation");
            }
            else
                RhinoApp.WriteLine("You must select an Operation");

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
        get {return new Guid("d5d32c70-1963-4eaa-8297-dc34ab992c79"); }
        }
    }
}