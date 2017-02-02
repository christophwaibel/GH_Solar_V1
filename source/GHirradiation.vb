'3 days SIN/COS interpolation       !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
'irradiation2.vb for more days. test showed, for 4 days it was worse  
'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
'TO DO: option wether to have averaged profile over the entire analysis mesh,
'               or to have profile for each sensor point


Imports System.Collections.Generic
Imports Grasshopper.Kernel
Imports Rhino.Geometry


Public Class GHirradiation
    Inherits GH_Component
    ''' <summary>
    ''' Initializes a new instance of the GHpvmodule class.
    ''' </summary>
    Public Sub New()
        MyBase.New("Irradiation", "Irradiation", _
                    "Irradiation depending on tilt and orientation, and filtered with 3 shadow factors (1. winter solstice, 2. equinox, 3. summer solstice)", _
                    "EnergyHubs", "Simulation")
    End Sub

    ''' <summary>
    ''' Registers all the input parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterInputParams(pManager As GH_Component.GH_InputParamManager)
        'pManager.AddNumberParameter("rad. weather", "rad. weather", "radiation from weatherfile", GH_ParamAccess.list)
        'pManager.AddNumberParameter("altitude", "altitude", "altitude from PSA algorithm", GH_ParamAccess.list)
        'pManager.AddVectorParameter("vecAlpha", "vecAlpha", "incident radiation (alpha) from PSA algorithm", GH_ParamAccess.list)
        'pManager.AddVectorParameter("vecBeta", "vecBeta", "avg. angle of module (beta)", GH_ParamAccess.item)
        'pManager.AddNumberParameter("shadow factors", "shadow factors", "shadow factors for 3 days: summer solstice, winter solstice, equinox", GH_ParamAccess.list)
        'pManager.AddBooleanParameter("day night", "day night", "day night", GH_ParamAccess.list)


        'DNI    is direct normal irradiation
        pManager.AddNumberParameter("DNI", "DNI", "Direct Normal Irradiation", GH_ParamAccess.list)
        'DHI    is diffuse horizontal irradiation
        pManager.AddNumberParameter("DHI", "DHI", "Diffuse Horizontal Irradiation", GH_ParamAccess.list)
        'φ      is the latitude of the location,
        pManager.AddNumberParameter("φ", "φ", "latitude of the location", GH_ParamAccess.item)
        'β      is module tilt, 
        ' pManager.AddNumberParameter("β", "β", "module tilt", GH_ParamAccess.item)
        'ψ      is module azimuth (orientation measured from South to West), 
        'pManager.AddNumberParameter("ψ", "ψ", "module azimuth (orientation measured from South to West)", GH_ParamAccess.item)
        'dd     is day of the year, Jan 1 is d=1
        pManager.AddIntegerParameter("alldays", "alldays", "all days of the year to be calculated, Jan 1 is d=1", GH_ParamAccess.list)
        'LT     is local time
        pManager.AddIntegerParameter("hours", "hours", "hours to be calculated", GH_ParamAccess.list)
        'dTgmt
        pManager.AddIntegerParameter("dTgmt", "dTgmt", "delta to GMT, 0-24", GH_ParamAccess.item)

        'shadowfactor objects
        pManager.AddGenericParameter("shadowObjsSummer", "shadowObjsSummer", "shadowObjsSummer, for direct radiation B (beam)", GH_ParamAccess.item)
        pManager.AddGenericParameter("shadowObjsWinter", "shadowObjsWinter", "shadowObjsWinter, for direct radiation B (beam)", GH_ParamAccess.item)
        pManager.AddGenericParameter("shadowObjsEquinox", "shadowObjsEquinox", "shadowObjsEquinox, for direct radiation B (beam)", GH_ParamAccess.item)
        pManager.AddNumberParameter("ShadingMask", "ShadingMask", "ShadingMask, for diffuse radiation D", GH_ParamAccess.item)
    End Sub

    ''' <summary>
    ''' Registers all the output parameters for this component.
    ''' </summary>
    Protected Overrides Sub RegisterOutputParams(pManager As GH_Component.GH_OutputParamManager)
        'pManager.AddNumberParameter("radiation on module", "radiation on module", "radiation on module", GH_ParamAccess.list)
        'pManager.AddNumberParameter("alphabeta", "alphabeta", "alphabeta", GH_ParamAccess.list)
        'pManager.AddNumberParameter("radFactor", "radFactor", "radFactor", GH_ParamAccess.list)


        pManager.AddNumberParameter("Gout", "Gout", "total irradiation on module", GH_ParamAccess.list)
    End Sub

    ''' <summary>
    ''' This is the method that actually does the work.
    ''' </summary>
    ''' <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    Protected Overrides Sub SolveInstance(DA As IGH_DataAccess)
        Dim rad As Double = Math.PI / 180




        ''////////////////////////////////////////////////////////////////////////////////
        ''////////////////////////////////////////////////////////////////////////////////
        ''///////////////////////////////////////////////////////////////    Method A
        'http://www.pveducation.org/pvcdrom/properties-of-sunlight/solar-radiation-on-tilted-surface

        'Dim Sh As New List(Of Double)                       '8760 in here. global horizontal radiation
        'If Not (DA.GetDataList(0, Sh)) Then Return
        'Dim alpha As New List(Of Double)                    'only day altitude from PSA (degree)
        'If Not (DA.GetDataList(1, alpha)) Then Return
        'Dim alphaVec As New List(Of Vector3d)           'only day vecs from PSA
        'If Not (DA.GetDataList(2, alphaVec)) Then Return
        'Dim betaVec As New Vector3d                     'one here. avg normal vector of mesh
        'If Not (DA.GetData(3, betaVec)) Then Return

        'Dim Sm As New List(Of Double)
        'Dim alphabeta(alphaVec.Count - 1) As Double   'construct difference between alphavec and betavec. 90 is no sun, 0 is full sun
        'Dim radiationFactor(alphaVec.Count - 1) As Double

        'Dim daynight As New List(Of Boolean)
        'If Not (DA.GetDataList(5, daynight)) Then Return

        'Dim counter As Integer = 0
        'For i As Integer = 0 To daynight.Count - 1
        '    If daynight(i) = True Then
        '        '  ReDim Preserve alphabeta(i)
        '        alphabeta(counter) = 90 - (Vector3d.VectorAngle(alphaVec(counter), betaVec) / rad)
        '        radiationFactor(counter) = Math.Sin(rad * alphabeta(counter)) / Math.Sin(rad * alpha(counter))
        '        Sm.Add(Sh(i) * radiationFactor(counter))
        '        If Sm(i) < 0 Then Sm(i) = 0
        '        counter = counter + 1
        '    Else
        '        Sm.Add(0)
        '    End If
        'Next

        'DA.SetDataList(0, Sm)
        'DA.SetDataList(1, alphabeta)
        'DA.SetDataList(2, radiationFactor)






        '////////////////////////////////////////////////////////////////////////////////
        '////////////////////////////////////////////////////////////////////////////////
        '///////////////////////////////////////////////////////////////    Method B
        'http://www.pveducation.org/pvcdrom/properties-of-sunlight/making-use-of-TMY
        '1. Luque A, Hegedus S. Handbook of Photovoltaic Science and Engineering. [Internet]. 2003 :1117.  
        Dim beta, delta, psi, phi, Bangle As Double
        Dim HRA, LST, TC, Longitude, LSTM, EoT, DHI, DNI As Double
        Dim LT, dd, dTgmt As Integer
        Dim B, D As Double
        Dim Dfactor As Double

        'DNI    is direct normal irradiation                                    0   list of dbl
        Dim DNIlist As New List(Of Double)
        If Not (DA.GetDataList(0, DNIlist)) Then Return

        'DHI    is diffuse horizontal irradiation                               1   list of dbl
        Dim DHIlist As New List(Of Double)
        If Not (DA.GetDataList(1, DHIlist)) Then Return

        'φ      is the latitude of the location,                                2   double
        If Not (DA.GetData(2, phi)) Then Return

        'β      is module tilt,                                                 3   double
        'If Not (DA.GetData(3, beta)) Then Return

        'ψ      is module azimuth (orientation measured from South to West),    4   double
        ' If Not (DA.GetData(4, psi)) Then Return

        'd      is day of the year, Jan 1 is d=1                                5   list of int
        Dim ddlist As New List(Of Integer)
        If Not (DA.GetDataList(3, ddlist)) Then Return

        'LT     is local time                                                   6   list of int
        Dim LTlist As New List(Of Integer)
        If Not (DA.GetDataList(4, LTlist)) Then Return

        'dTgmt  difference Local Time (LT) from Greenwich Mean Time (GMT)       7   integer
        If Not (DA.GetData(5, dTgmt)) Then Return


        Dim _shadowsummer As ShadowFactor
        If Not (DA.GetData(6, _shadowsummer)) Then Return
        Dim _shadowwinter As ShadowFactor
        If Not (DA.GetData(7, _shadowwinter)) Then Return
        Dim _shadowequinox As ShadowFactor
        If Not (DA.GetData(8, _shadowequinox)) Then Return

        beta = _shadowsummer.beta
        psi = _shadowsummer.psi

        'Dfactor is diffuse radiation reduction factor, coming from Shading Mask or if not, then from simplified calculation
        If Not (DA.GetData(9, Dfactor)) Then Dfactor = ((180 - beta) / 180)



        'where:
        'B      is direct (beam) irradiation
        'D      is diffuse irradiation
        'δ      is the Declination Angle, 
        'φ      is the latitude of the location,
        'β      is module tilt, 
        'ψ      is module azimuth (orientation measured from South to West), 
        'HRA    is hour angle, discussed on the page Solar Time.
        'DNI    is direct normal irradiation
        'DHI    is diffuse horizontal irradiation
        'dd     is day of the year, Jan 1 is d=1
        'HRA    is Hour Angle (HRA)
        'LT     is local time
        'LST    is Local Solar Time (LST)
        'TC     is Time Correction Factor (TC)
        'EoT    is Equation of Time (EoT)
        'Bangle is some angle, depending on the day
        'LSTM   is Local Standard Time Meridian (LSTM)
        'dTgmt  is difference of the Local Time (LT) from Greenwich Mean Time (GMT) in hours. 15°= 360°/24 hours


        Dim Glist As New List(Of Double)
        Dim Blist As New List(Of Double)
        Dim Dlist As New List(Of Double)

        Dim counter As Integer = 0
        For Each dd In ddlist
            For Each LT In LTlist

                DHI = DHIlist(counter)
                DNI = DNIlist(counter)
                '////////////////////////////////////////////////////////////////////////////////
                'Local Standard Time Meridian (LSTM)
                'LSTM = 15° * dTgmt
                LSTM = 15 * dTgmt

                '////////////////////////////////////////////////////////////////////////////////
                'Equation of Time (EoT)
                Bangle = (360 / 365) * (dd - 81)      'in degrees
                EoT = 9.87 * Math.Sin(rad * (2 * Bangle)) - 7.53 * Math.Cos(rad * Bangle) - 1.5 * Math.Sin(rad * Bangle)



                '////////////////////////////////////////////////////////////////////////////////
                'Time Correction Factor (TC)
                TC = 4 * (Longitude - LSTM) + EoT

                '////////////////////////////////////////////////////////////////////////////////
                'Local Solar Time (LST)
                LST = LT + (TC / 60)
                'where:
                'LT is local time

                '////////////////////////////////////////////////////////////////////////////////
                'Hour Angle (HRA)
                'HRA = 15°(LST-12)
                HRA = 15 * (LST - 12)

                '////////////////////////////////////////////////////////////////////////////////
                'declination angle
                'δ=sin^−1(sin(23.45°)sin((360/365)*(d−81)))
                delta = Math.Asin(Math.Sin(rad * 23.45) * Math.Sin(rad * ((360 / 365) * (dd - 81))))
                'd is day of the year, Jan 1 is d=1

                '////////////////////////////////////////////////////////////////////////////////
                'D = DHI ((180-β)/180)
                D = DHI * Dfactor
                'where:
                'DHI is diffuse horizontal irradiation

                '////////////////////////////////////////////////////////////////////////////////
                'B = DNI(sin(δ)sin(φ)cos(β) - 
                'sin(δ)cos(φ)sin(β)cos(ψ) + 
                'cos(δ)cos(φ)cos(β)cos(HRA) +
                'cos(δ)sin(φ)sin(β)cos(ψ)cos(HRA) + 
                'cos(δ)sin(ψ)sin(HRA)sin(β)
                B = DNI * (Math.Sin(rad * delta) * Math.Sin(rad * phi) * Math.Cos(rad * beta) - _
                         Math.Sin(rad * delta) * Math.Cos(rad * phi) * Math.Sin(rad * beta) * Math.Cos(rad * psi) + _
                         Math.Cos(rad * delta) * Math.Cos(rad * phi) * Math.Cos(rad * beta) * Math.Cos(rad * HRA) + _
                         Math.Cos(rad * delta) * Math.Sin(rad * phi) * Math.Sin(rad * beta) * Math.Cos(rad * psi) * Math.Cos(rad * HRA) + _
                         Math.Cos(rad * delta) * Math.Sin(rad * psi) * Math.Sin(rad * HRA) * Math.Sin(rad * beta))
                If B < 0 Then B = 0 'don't know why it can have minus values...
                'where:
                'δ is the Declination Angle, 
                'φ is the latitude of the location,
                'β is module tilt, 
                'ψ is module azimuth (orientation measured from South to West), 
                'and HRA is hour angle, discussed on the page Solar Time.
                'DNI is direct normal irradiation

                '////////////////////////////////////////////////////////////////////////////////
                'G = B + D
                'where:
                'B is direct (beam)
                'D is diffuse



                counter = counter + 1
                Blist.Add(B)
                Dlist.Add(D)
                '                Glist.Add(G)
            Next
        Next


        Blist = InterpolateShadowDays(Blist, _shadowsummer, _shadowwinter, _shadowequinox)

        For i = 0 To Dlist.Count - 1
            Glist.Add(Blist(i) + Dlist(i))
        Next
        DA.SetDataList(0, Glist)

    End Sub

    'interpolating all 8760 between correct dates equinox, summer, winter
    Private Function InterpolateShadowDays(radList As List(Of Double), _
                                           _shadowsummer As ShadowFactor, _
                                           _shadowwinter As ShadowFactor, _
                                           _shadowequinox As ShadowFactor) As List(Of Double)

        '100% equinox is 20 march and 23 Sept
        '100% summer is 21 june
        '100% winter is 22 decemebr

        Dim y1, y2, y3, y4, y5, y6 As Integer
        y1 = -9     'winter solstice
        y2 = 78     'equinox spring
        y3 = 171    'summer solstice
        y4 = 265    'equinox autumn
        y5 = 355    'winter solstice
        y6 = 443    'equinox spring



        Dim fullF1, fullF2 As Integer


        Dim factor As Double = 1
        Dim i, u As Integer
        Dim InterpolInterv As Integer
        Dim dist1, dist2 As Double

        fullF1 = y1                    '100% shadowwinter on this day
        fullF2 = y2                     '100% shadowequinox on this day
        InterpolInterv = y2 + (y1 * -1)
        For i = 0 To y2 - 1             'from 0.Jan to 19.March
            dist1 = (InterpolInterv - Math.Abs(fullF1 - i)) / InterpolInterv
            dist1 = 1 - dist1
            dist1 = Math.Cos(dist1 * (0.5 * Math.PI))
            'dist2 = (InterpolInterv - Math.Abs(fullF2 - i)) / InterpolInterv
            dist2 = 1 - dist1
            For u = 0 To 23
                factor = ((1 - Math.Round(_shadowwinter.ShadowFactors(u), 4)) * dist1) * _shadowwinter.sunshine(u) + _
                    ((1 - Math.Round(_shadowequinox.ShadowFactors(u), 4)) * dist2) * _shadowequinox.sunshine(u)

                radList(i * 24 + u) = radList(i * 24 + u) * factor
                If factor > 1 Then
                    factor = 1
                End If
            Next
        Next

        fullF1 = y2                      '100% shadowequinox on this day
        fullF2 = y3                     '100% shadowsummer on this day
        InterpolInterv = y3 - y2
        For i = y2 To y3 - 1            'from 20.March to 20.june
            dist1 = (InterpolInterv - Math.Abs(fullF1 - i)) / InterpolInterv
            'dist1 = 1 - dist1
            dist1 = Math.Sin(dist1 * (0.5 * Math.PI))
            'dist2 = (InterpolInterv - Math.Abs(fullF2 - i)) / InterpolInterv
            dist2 = 1 - dist1
            For u = 0 To 23
                factor = ((1 - Math.Round(_shadowequinox.ShadowFactors(u), 4)) * dist1) * _shadowequinox.sunshine(u) + _
                   ((1 - Math.Round(_shadowsummer.ShadowFactors(u), 4)) * dist2) * _shadowsummer.sunshine(u)
                radList(i * 24 + u) = radList(i * 24 + u) * factor
                If factor > 1 Then
                    factor = 1
                End If

            Next
        Next

        fullF1 = y3                      '100% shadowsummer on this day
        fullF2 = y4                     '100% shadowequinox on this day
        InterpolInterv = y4 - y3
        For i = y3 To y4 - 1            'from 21.June to 22.Sept
            dist1 = (InterpolInterv - Math.Abs(fullF1 - i)) / InterpolInterv
            dist1 = 1 - dist1
            dist1 = Math.Cos(dist1 * (0.5 * Math.PI))
            'dist2 = (InterpolInterv - Math.Abs(fullF2 - i)) / InterpolInterv
            dist2 = 1 - dist1
            For u = 0 To 23
                factor = ((1 - Math.Round(_shadowsummer.ShadowFactors(u), 4)) * dist1) * _shadowsummer.sunshine(u) + _
                   ((1 - Math.Round(_shadowequinox.ShadowFactors(u), 4)) * dist2) * _shadowequinox.sunshine(u)
                radList(i * 24 + u) = radList(i * 24 + u) * factor
                If factor > 1 Then
                    factor = 1
                End If
            Next
        Next

        fullF1 = y4                      '100% shadowequinox on this day
        fullF2 = y5                     '100% shadowwinter on this day
        InterpolInterv = y5 - y4
        For i = y4 To y5 - 1            'from 23.Sept to 21.Dec
            dist1 = (InterpolInterv - Math.Abs(fullF1 - i)) / InterpolInterv
            'dist1 = 1 - dist1
            dist1 = Math.Sin(dist1 * (0.5 * Math.PI))
            'dist2 = (InterpolInterv - Math.Abs(fullF2 - i)) / InterpolInterv
            dist2 = 1 - dist1
            For u = 0 To 23
                factor = ((1 - Math.Round(_shadowequinox.ShadowFactors(u), 4)) * dist1) * _shadowequinox.sunshine(u) + _
                   ((1 - Math.Round(_shadowwinter.ShadowFactors(u), 4)) * dist2) * _shadowwinter.sunshine(u)
                radList(i * 24 + u) = radList(i * 24 + u) * factor
                If factor > 1 Then
                    factor = 1
                End If
            Next
        Next

        fullF1 = y5                      '100% shadowwinter on this day
        fullF2 = y6                     '100% shadowequinox spring on this day
        InterpolInterv = y6 - y5
        For i = y5 To 364            'from 22.Dec to 31.Dec
            dist1 = (InterpolInterv - Math.Abs(fullF1 - i)) / InterpolInterv
            dist1 = 1 - dist1
            dist1 = Math.Cos(dist1 * (0.5 * Math.PI))
            'dist2 = (InterpolInterv - Math.Abs(fullF2 - i)) / InterpolInterv
            dist2 = 1 - dist1
            For u = 0 To 23
                factor = ((1 - Math.Round(_shadowwinter.ShadowFactors(u), 4)) * dist1) * _shadowwinter.sunshine(u) + _
                   ((1 - Math.Round(_shadowequinox.ShadowFactors(u), 4)) * dist2) * _shadowequinox.sunshine(u)
                radList(i * 24 + u) = radList(i * 24 + u) * factor
                If factor > 1 Then
                    factor = 1
                End If
            Next
        Next



        Return radList
    End Function

    ''' <summary>
    ''' Provides an Icon for every component that will be visible in the User Interface.
    ''' Icons need to be 24x24 pixels.
    ''' </summary>
    Protected Overrides ReadOnly Property Icon() As System.Drawing.Bitmap
        Get
            'You can add image files to your project resources and access them like this:
            ' return Resources.IconForThisComponent;
            Return My.Resources.PVmodule
        End Get
    End Property

    ''' <summary>
    ''' Gets the unique ID for this component. Do not change this ID after release.
    ''' </summary>
    Public Overrides ReadOnly Property ComponentGuid() As Guid
        Get
            Return New Guid("{fcc6a003-6c0d-4c1b-bb7e-0f02326c0d8f}")
        End Get
    End Property
End Class