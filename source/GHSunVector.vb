Imports System.Collections.Generic

Imports Grasshopper.Kernel
Imports Rhino.Geometry


Public Class GHSunVector
    Inherits GH_Component
    ''' <summary>
    ''' Initializes a new instance of the GHSunVector class.
    ''' </summary>
    Public Sub New()
        MyBase.New("SunVector", "SunVector", _
                    "SunVector Calculation", _
                    "EnergyHubs", "Simulation")
    End Sub

    ''' <summary>
    ''' weather file .epw -> extract latitude and longitude from here
    ''' year
    ''' month
    ''' day
    ''' hour
    ''' if no weather file: latitude and longitude
    ''' </summary>
    Protected Overrides Sub RegisterInputParams(pManager As GH_Component.GH_InputParamManager)

        pManager.AddIntegerParameter("year", "year", "year", GH_ParamAccess.item)
        pManager.AddIntegerParameter("month", "month", "month", GH_ParamAccess.item)
        pManager.AddIntegerParameter("day", "day", "day", GH_ParamAccess.item)
        pManager.AddIntegerParameter("hour", "hour", "hour", GH_ParamAccess.list)
        pManager.AddNumberParameter("longitude", "longitude", "longitude", GH_ParamAccess.item)
        pManager.AddNumberParameter("latitude", "latitude", "latitude", GH_ParamAccess.item)
        pManager.AddBooleanParameter("whole year?", "whole year?", "whole year?", GH_ParamAccess.item)
    End Sub

    ''' <summary>
    ''' Registers all the output parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterOutputParams(pManager As GH_Component.GH_OutputParamManager)
        pManager.AddVectorParameter("vec", "vec", "vec", GH_ParamAccess.list)
        ' pManager.AddLineParameter("vec", "vec", "vec", GH_ParamAccess.list)
        ' pManager.AddTextParameter("day night", "day night", "day night", GH_ParamAccess.item)
        pManager.AddNumberParameter("azimuth", "azimuth", "azimuth", GH_ParamAccess.list)
        pManager.AddNumberParameter("altitude", "altitude", "altitude", GH_ParamAccess.list)
        pManager.AddBooleanParameter("day or night", "day or night", "day or night", GH_ParamAccess.list)
    End Sub

    ''' <summary>
    ''' This is the method that actually does the work.
    ''' </summary>
    ''' <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)


        Dim _y As Integer               'years
        If (Not DA.GetData(0, _y)) Then Return

        Dim wholeyear As Boolean
        If (Not DA.GetData(6, wholeyear)) Then Return

        Dim _longitude As Double
        If (Not DA.GetData(4, _longitude)) Then Return
        Dim _latitude As Double
        If (Not DA.GetData(5, _latitude)) Then Return


        Dim vecs() As Vector3d
        Dim azims() As Double
        Dim altis() As Double
        Dim counter As Integer = 0
        Dim daynight As New List(Of Boolean)


        If wholeyear = True Then                                'whole year
            '//////////////////////////////////////////////////////////////////////////
            '//////////////////////////////////////////////////////////////////////////
            Dim sunvec As SunVector

            For _m As Integer = 1 To 12
                Dim daysInMonth As Integer = System.DateTime.DaysInMonth(_y, _m)
                For _d As Integer = 1 To daysInMonth
                    For i As Integer = 1 To 24
                        sunvec = New SunVector(_y, _m, _d, i, 0, 0, _longitude, _latitude)

                        Dim vec As New Vector3d(sunvec.udtCoordXYZ.x, sunvec.udtCoordXYZ.y, sunvec.udtCoordXYZ.z)
                        If sunvec.udtCoordinates.dZenithAngle <= 90 Then
                            'ReDim Preserve lines(counter)
                            'lines(counter) = line
                            ReDim Preserve vecs(counter)
                            ReDim Preserve azims(counter)
                            ReDim Preserve altis(counter)
                            vecs(counter) = vec
                            azims(counter) = sunvec.udtCoordinates.dAzimuth
                            altis(counter) = 90 - sunvec.udtCoordinates.dZenithAngle
                            'DA.SetData(1, "day")
                            counter = counter + 1
                            daynight.Add(True)
                        Else
                            daynight.Add(False)
                        End If

                    Next
                Next
            Next







        Else                                                    'one specific day
            '//////////////////////////////////////////////////////////////////////////
            '//////////////////////////////////////////////////////////////////////////


            Dim _m As Integer                               'months
            If (Not DA.GetData(1, _m)) Then Return
            Dim _d As Integer                               'days
            If (Not DA.GetData(2, _d)) Then Return
            Dim _h As New List(Of Integer)                  'hours
            If (Not DA.GetDataList(3, _h)) Then Return



            Dim sunvec As SunVector
            Dim _hitem As Integer               'one hour
            'Dim lines() As Line

            For Each _hitem In _h
                sunvec = New SunVector(_y, _m, _d, _hitem, 0, 0, _longitude, _latitude)
                'Dim line As New Line(0, 0, 0, sunvec.udtCoordXYZ.x, sunvec.udtCoordXYZ.y, sunvec.udtCoordXYZ.z)
                Dim vec As New Vector3d(sunvec.udtCoordXYZ.x, sunvec.udtCoordXYZ.y, sunvec.udtCoordXYZ.z)
                If sunvec.udtCoordinates.dZenithAngle <= 90 Then
                    'ReDim Preserve lines(counter)
                    'lines(counter) = line
                    ReDim Preserve vecs(counter)
                    ReDim Preserve azims(counter)
                    ReDim Preserve altis(counter)
                    vecs(counter) = vec
                    azims(counter) = sunvec.udtCoordinates.dAzimuth
                    altis(counter) = 90 - sunvec.udtCoordinates.dZenithAngle
                    'DA.SetData(1, "day")
                    counter = counter + 1
                    daynight.Add(True)
                Else
                    daynight.Add(False)
                End If

            Next
        End If

        
        If counter > 0 Then
            DA.SetDataList(0, vecs)
            'DA.SetDataList(0, lines)
            DA.SetDataList(1, azims)    'azemuth
            DA.SetDataList(2, altis)    'altitude
            DA.SetDataList(3, daynight)
        End If

    End Sub

    ''' <summary>
    ''' Provides an Icon for every component that will be visible in the User Interface.
    ''' Icons need to be 24x24 pixels.
    ''' </summary>
    Protected Overrides ReadOnly Property Icon() As System.Drawing.Bitmap
        Get
            Return My.Resources.sunvec
        End Get
    End Property

    ''' <summary>
    ''' Gets the unique ID for this component. Do not change this ID after release.
    ''' </summary>
    Public Overrides ReadOnly Property ComponentGuid() As Guid
        Get
            Return New Guid("{cd3dc6b9-1351-479c-a8fc-97aed8f84b03}")
        End Get
    End Property
End Class