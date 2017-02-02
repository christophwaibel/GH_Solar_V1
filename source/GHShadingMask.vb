Imports System.Collections.Generic

Imports Grasshopper.Kernel
Imports Rhino.Geometry
Imports Grasshopper.Kernel.Parameters


Public Class GHShadingMask
    Inherits GH_Component
    ''' <summary>
    ''' Initializes a new instance of the GHShadingMask class.
    ''' </summary>
    Public Sub New()
        MyBase.New("Shading Mask", "Shading Mask", _
                     "Shading Mask Simulation for diffuse radiation", _
                     "EnergyHubs", "Simulation")
    End Sub

    ''' <summary>
    ''' Registers all the input parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterInputParams(pManager As GH_Component.GH_InputParamManager)
        pManager.AddMeshParameter("MeshCheckSun", "mshSimulate (List)", "Input (List of) Mesh, which shall be checked for Radiation", GH_ParamAccess.list)
        pManager.AddMeshParameter("MeshObstacles", "mshObstacles (List)", "Input (List of) Mesh, which are Obstacles", GH_ParamAccess.list)
        pManager.AddIntegerParameter("Resolution of SkyDome", "intResolution", "Multiplication factor for resolution of SkyDome, Integer. The higher the number, the higher the amount of patches in SkyDome", GH_ParamAccess.item, 1)
        pManager(2).Optional = True
        pManager.AddBooleanParameter("Simplified calculation of Shading Mask?", "blnSimplified", "Simplified calculation of Shading Mask, using just the average mesh normal of the panel, instead of all normals", GH_ParamAccess.item, True)
        pManager(3).Optional = True
    End Sub

    ''' <summary>
    ''' Registers all the output parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterOutputParams(pManager As GH_Component.GH_OutputParamManager)
        'shading mask factors for each mesh. just one number per mesh
        pManager.AddNumberParameter("shading mask factor", "shading mask factor", "shading mask factor", GH_ParamAccess.list)
        pManager.AddMeshParameter("dome", "dome", "dome", GH_ParamAccess.list)
    End Sub

    ''' <summary>
    ''' This is the method that actually does the work.
    ''' </summary>
    ''' <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)
        Dim mshSim As New List(Of Mesh)
        If (Not DA.GetDataList(0, mshSim)) Then Return

        Dim mshObs As New List(Of Mesh)
        If (Not DA.GetDataList(1, mshObs)) Then Return

        Dim intResolution As Integer
        If (Not DA.GetData(2, intResolution)) Then intResolution = 1

        Dim blnSimple As Boolean
        If (Not DA.GetData(3, blnSimple)) Then blnSimple = True

        Dim DfactorList As New List(Of Double)
        Dim domlist As New List(Of Mesh)
        For i As Integer = 0 To mshSim.Count - 1
            Dim mask As New ShadingMask(mshSim(i), mshObs, intResolution, blnSimple)
            domlist.Add(mask.domeOut)
            DfactorList.Add(mask.ReductionFactor)
        Next

        DA.SetDataList(0, DfactorList)
        DA.SetDataList(1, domlist)
    End Sub

    ''' <summary>
    ''' Provides an Icon for every component that will be visible in the User Interface.
    ''' Icons need to be 24x24 pixels.
    ''' </summary>
    Protected Overrides ReadOnly Property Icon() As System.Drawing.Bitmap
        Get
            'You can add image files to your project resources and access them like this:
            ' return Resources.IconForThisComponent;
            Return My.Resources.shadingMask
        End Get
    End Property

    ''' <summary>
    ''' Gets the unique ID for this component. Do not change this ID after release.
    ''' </summary>
    Public Overrides ReadOnly Property ComponentGuid() As Guid
        Get
            Return New Guid("{27c25646-81e0-490e-b7d4-d0d1db1377e3}")
        End Get
    End Property
End Class