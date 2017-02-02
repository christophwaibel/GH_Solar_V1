Imports System.Collections.Generic

Imports Grasshopper.Kernel
Imports Rhino.Geometry


Public Class GHShadowFactors
    Inherits GH_Component
    ''' <summary>
    ''' Initializes a new instance of the GHRadiation class.
    ''' </summary>
    Public Sub New()
        MyBase.New("Shadow Factors", "Shadow Factors", _
                    "Shadow Factors Simulation for direct radiation", _
                    "EnergyHubs", "Simulation")
    End Sub

    ''' <summary>
    ''' Registers all the input parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterInputParams(pManager As GH_Component.GH_InputParamManager)
        pManager.AddMeshParameter("MeshCheckSun", "mshSimulate (List)", "Input (List of) Mesh, which shall be checked for Radiation", GH_ParamAccess.list)
        pManager.AddMeshParameter("MeshObstacles", "mshObstacles (List)", "Input (List of) Mesh, which are Obstacles", GH_ParamAccess.list)
        pManager.AddVectorParameter("sunvecs", "sunvecs", "sunvecs", GH_ParamAccess.list)
        pManager.AddBooleanParameter("dayornight", "dayornight", "dayornight", GH_ParamAccess.list)
       
    End Sub

    ''' <summary>
    ''' Registers all the output parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterOutputParams(pManager As GH_Component.GH_OutputParamManager)
        pManager.AddMeshParameter("Coloured Mesh", "mshColoured", "Output Mesh, coloured according to simulation results", GH_ParamAccess.list)
        pManager.AddGenericParameter("ShadowFactors", "dblShadows (List)", "Percentage of mesh area blocked from sun", GH_ParamAccess.list)
        '   pManager.AddNumberParameter("total kWh", "dblTotalkWh", "total kWh of all input meshes, for the simulated day", GH_ParamAccess.item)

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


        Dim v3ds As New List(Of Vector3d)
        If (Not DA.GetDataList(2, v3ds)) Then Return


        Dim DoN As New List(Of Boolean)
        If (Not DA.GetDataList(3, DoN)) Then Return


        Dim ColMesh As New List(Of Mesh)
        Dim ShadowList As New List(Of ShadowFactor)


        For i As Integer = 0 To mshSim.Count - 1
            Dim shadow As New ShadowFactor()
            shadow.RunScript(mshSim(i), mshObs, v3ds, DoN)
            'shadow.beta = _beta(i)
            'shadow.psi = _psi(i)
            ShadowList.Add(shadow)
            ColMesh.Add(shadow.ColouredMesh)
        Next
        'rad = New ShadowFactor()
        'rad.RunScript(mshSim, mshObs, v3ds)
        'ColMesh = rad.ColouredMesh
        ' ShadowFactors = rad.ShadowFactors 



        DA.SetDataList(0, ColMesh)
        DA.SetDataList(1, ShadowList)
        ' DA.SetData(2, rad.Fitness)



    End Sub

    ''' <summary>
    ''' Provides an Icon for every component that will be visible in the User Interface.
    ''' Icons need to be 24x24 pixels.
    ''' </summary>
    Protected Overrides ReadOnly Property Icon() As System.Drawing.Bitmap
        Get
            Return My.Resources.shadow
        End Get
    End Property

    ''' <summary>
    ''' Gets the unique ID for this component. Do not change this ID after release.
    ''' </summary>
    Public Overrides ReadOnly Property ComponentGuid() As Guid
        Get
            Return New Guid("{87e6746c-9954-405f-9d2b-07f25f43fc6c}")
        End Get
    End Property
End Class