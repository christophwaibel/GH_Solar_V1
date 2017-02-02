Imports Grasshopper.Kernel
Imports Rhino.Geometry
Imports System.Drawing

'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
' TO DO: Add feature: reflectivity (optional), set bounces. need to define surface reflectance of obstacles then
'           only bounces for reflective surfaces..
' This is for DIRECT. do the same for DIFFUSE
Public Class ShadowFactor

    Public ColouredMesh As Mesh
    Public Fitness As Double
    Public ShadowFactors As New List(Of Double)
    Public beta As Double   'tilt angle
    Public psi As Double    'azimuth angle
    Public sunshine(23) As Integer

    Public Sub RunScript(ByVal MeshCheckSun As Mesh, _
                         ByVal MeshObstacles As List(Of Mesh), _
                         ByVal v3ds As List(Of Vector3d), _
                         ByVal don As List(Of Boolean)) 'replace strSeason with list of vectors

        Dim rad = Math.PI / 180


        Dim i As Integer
        Dim arrMeshObst(MeshObstacles.Count - 1) As Mesh
        For i = 0 To MeshObstacles.Count - 1
            arrMeshObst(i) = MeshObstacles(i)
        Next

        Dim _calc(2) As Object
        _calc = SunShadowHeat(MeshCheckSun, arrMeshObst, v3ds)

        '     Print("FITNESS " & _fit)
        ColouredMesh = _calc(0)
        Fitness = _calc(1)

        Dim counter As Integer = 0
        For i = 0 To don.Count - 1
            If don(i) = True Then
                ShadowFactors.Add(_calc(2)(counter))
                counter = counter + 1
                sunshine(i) = 1
            Else
                ShadowFactors.Add(1)
                sunshine(i) = 0
            End If

        Next




        Dim avgnormal As Vector3d = avgMeshNormal(MeshCheckSun)
        Dim betaangle As New Vector3d(0, 0, 1)
        Dim psiangle As New Vector3d(0, -1, 0)
        Dim psiplane As New Plane(New Point3d(0, 0, 0), New Vector3d(0, 0, 1))
        beta = (Vector3d.VectorAngle(avgnormal, betaangle) / rad)
        psi = (Vector3d.VectorAngle(avgnormal, psiangle, psiplane) / rad)

        If psi = Nothing Or Double.IsNegativeInfinity(psi) Or Double.IsNaN(psi) Or Double.IsInfinity(psi) Then
            psi = 0
        End If




        'Dim _fit As Double = 0
        'Dim _calc(2) As Object
        'Dim ColMesh As New List(Of Mesh)
        'Dim cm As Mesh
        'Dim _vecs() As Double
        'Dim __vecs As New List(Of Double)      'should be somehow a matrix
        'For i = 0 To MeshCheckSun.Count - 1
        '    _calc = SunShadowHeat(MeshCheckSun.Item(i), arrMeshObst, v3ds)
        '    cm = _calc(0)
        '    ColMesh.Add(cm)

        '    _vecs = _calc(2)
        '    For n = 0 To UBound(_vecs)
        '        __vecs.Add(_vecs(n))
        '    Next
        '    _fit = _fit + _calc(1)
        'Next


        ''     Print("FITNESS " & _fit)
        'ColouredMesh = ColMesh
        'Fitness = _fit

        'ShadowFactors = __vecs

    End Sub


    Function SunShadowHeat(ByVal strMesh As Mesh, ByVal obstacle() As Mesh, ByVal v3d As List(Of Vector3d))
        Dim i, n As Integer

        Const SolarConstant As Double = 1367 * 0.78 'W/sqm....solar constant minus 16% absorbed atmosphere and 6% reflected atmosphere
        '____________________________________________________________________________________________________________________________
        '////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        '////////////////////////////////////////////																	SUN VECTORS
        Dim VecSun() As Vector3d
        Dim VecFaceAlpha() As Double
        ReDim VecSun(v3d.Count - 1)

        Dim ShadowAccount(UBound(VecSun)) As Double     'percentage of mesh area blocked from sun

        For i = 0 To UBound(VecSun)
            VecSun(i).X = v3d(i).X
            VecSun(i).Y = v3d(i).Y
            VecSun(i).Z = v3d(i).Z

            ShadowAccount(i) = 0
        Next
        ReDim VecFaceAlpha(UBound(VecSun))
        '______________________________________________________________________________//////////////////////////////////////////////



        '____________________________________________________________________________________________________________________________
        '////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        '////////////////////////////////////////////														CALCULATING SUN+SHADOW

        Dim cen(strMesh.Faces.Count) As Point3d
        Dim F(strMesh.Faces.Count - 1)() As Integer
        '    Dim bool As Boolean
        '    Dim temp1, temp2, temp3, temp4 As point3f
        '    Dim temparr(3) As point3f
        For i = 0 To strMesh.Faces.Count - 1
            cen(i) = strMesh.Faces.GetFaceCenter(i)
            F(i) = New Integer(3) {}
            F(i)(0) = strMesh.Faces.GetFace(i).A
            F(i)(1) = strMesh.Faces.GetFace(i).B
            F(i)(2) = strMesh.Faces.GetFace(i).C
            F(i)(3) = strMesh.Faces.GetFace(i).D
            'Print(F(i)(0) & " " & F(i)(1) & " " & F(i)(2) & " " & F(i)(3))
            '      bool = strMesh.Faces.GetFaceVertices(i, temp1, temp2, temp3, temp4)
            '      temparr(0) = temp1
            '      temparr(1) = temp2
            '      temparr(2) = temp3
            '      temparr(3) = temp4
            '      F(i) = temparr
            '      'Print(MVert(i)(1).X)
        Next

        Dim MVertNorm(strMesh.Vertices.Count - 1) As Vector3f
        Dim MVert(strMesh.Vertices.Count - 1) As Point3f
        ' Dim tempv2 As Point3f
        ' Dim tempv As vector3f
        For i = 0 To strMesh.Vertices.Count - 1
            '   tempv = strMesh.normals.item(i)
            MVertNorm(i) = strMesh.Normals.Item(i)
            '  tempv2 = strMesh.Vertices.item(i)
            MVert(i) = strMesh.Vertices.Item(i)
            ' Print(MVert(i).X & " " & MVert(i).Y & " " & MVert(i).Z)
            ' Print(tempv.X & " " & tempv.Y & " " & tempv.Z)
        Next




        Dim ShadowVertex(UBound(MVert))() As Integer      'wether a vertex is blocked of sun (true) or not (false)

        Dim arrSunPStart, arrOffsetP As Point3d
        Dim VecOff As Vector3f
        Dim IntersectCheck, IntC2 As Double
        Dim kkk As Integer = 0
        Dim raySunCrvs As Ray3d
        Dim alpha As Double
        Dim alphaAcc(UBound(MVert)), alphaAvrg(UBound(MVert)) As Double
        Dim R, G, B As Double
        Dim c(UBound(MVert)) As Color

        '  Dim alphaAccSunVec(UBound(MVert))() As Double

        For i = 0 To UBound(MVert)
            arrSunPStart = New Point3d(MVert(i).X, MVert(i).Y, MVert(i).Z)
            VecOff = MVertNorm(i)
            VecOff.Unitize()
            VecOff = Vector3f.Multiply(VecOff, 0.1)
            arrOffsetP = Point3d.Add(VecOff, MVert(i))
            'Print(MVertNorm(i).Length & " " & VecOff.Length)

            ShadowVertex(i) = New Integer(UBound(VecSun)) {}
            ' alphaAccSunVec(i) = New Double(UBound(VecSun)) {}
            For n = 0 To UBound(VecSun)

                raySunCrvs = New Ray3d(arrOffsetP, VecSun(n))
                'Print(raySunCrvs.Direction.ToString)
                IntersectCheck = Intersect.Intersection.MeshRay(strMesh, raySunCrvs)      'checks self-blocking to sun (like an overhang or so)
                'Print(IntersectCheck)
                For kkk = 0 To UBound(obstacle)
                    IntC2 = Intersect.Intersection.MeshRay(obstacle(kkk), raySunCrvs)
                    If IntC2 >= 0 Then
                        kkk = UBound(obstacle)
                    End If
                Next

                If IntersectCheck < 0 And IntC2 < 0 Then
                    '          intShadowAccount(i) = intShadowAccount(i) + 1
                    alpha = Vector3d.VectorAngle(MVertNorm(i), VecSun(n))
                    alpha = alpha * 180 / Math.PI
                    ' Print("oho " & alpha)
                    If alpha > 90 Then
                        alphaAcc(i) = alphaAcc(i) + 90
                        alpha = 90
                        ShadowVertex(i)(n) = 1          'no sun on this vertex
                    Else
                        alphaAcc(i) = alphaAcc(i) + alpha
                        ShadowVertex(i)(n) = 0        'sun on this vertex
                    End If
                Else
                    alphaAcc(i) = alphaAcc(i) + 90
                    alpha = 90
                    ShadowVertex(i)(n) = 1              'no sun on this vertex
                End If

                ' alphaAccSunVec(i)(n) = alpha
            Next

            '      For n = 0 To UBound(VecSun)
            '        Print(n & ": " & alphaAccSunVec(i)(n))
            '      next



            alphaAvrg(i) = alphaAcc(i) / (UBound(VecSun) + 1)

            If alphaAvrg(i) > 45 And alphaAvrg(i) <= 67.5 Then
                R = 255
                G = 255 - ((alphaAvrg(i) - 45) * (255 / 22.5))
                B = 0
                'Print(alphaAvrg(i) & "     R: " & R & "   G: " & G & "    B: " & B)
            ElseIf alphaAvrg(i) > 67.5 Then
                R = 255 - ((alphaAvrg(i) - 67.5) * (255 / 22.5))
                G = 0
                B = ((alphaAvrg(i) - 67.5) * (255 / 22.5))
                ' Print(alphaAvrg(i) & "     R: " & R & "   G: " & G & "    B: " & B)
            Else
                R = 255
                G = 255
                B = 0
                ' Print(alphaAvrg(i) & "     R: " & R & "   G: " & G & "    B: " & B)
            End If



            '      Print("R " & int(R) & " G " & int(G) & " B " & int(B))
            '      Print(alphaAvrg(i))

            c(i) = Color.FromArgb(Int(R), Int(G), Int(B))
            'Print(c(i).R.ToString & " " & c(i).G.ToString & " " & c(i).B.ToString)
            'Print(alphaacc(i))
        Next


        '    '______________________________________________________________________________//////////////////////////////////////////////






        '    '____________________________________________________________________________________________________________________________
        '    '////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        '    '////////////////////////////////////////////																		FITNESS
        '    Dim tempMesh, tempArea, tempAvrg, temp0, temp1, temp2, temp3
        '    Dim tempFSunAcc, totalSunAcc
        '
        '
        '    Dim tempAvrg2, tempFSunAcc2
        '
        '

        Dim tempArea As Double
        Dim tempMesh As Mesh
        Dim tempAvrg, totalSunAcc, tempRad As Double
        Dim temp0, temp1, temp2, temp3 As Integer
        '       Dim alphaAccPerWholeMeshAndSunVec(UBound(VecSun)) As Double

        For n = 0 To strMesh.Faces.Count - 1
            tempMesh = New Mesh()
            tempMesh.Vertices.Add(MVert(F(n)(0)))
            tempMesh.Vertices.Add(MVert(F(n)(1)))
            tempMesh.Vertices.Add(MVert(F(n)(2)))
            tempMesh.Vertices.Add(MVert(F(n)(3)))
            tempMesh.Faces.AddFace(0, 1, 2, 3)
            tempArea = AreaMassProperties.Compute(tempMesh).Area
            'Print("Area ni sqm:   " & tempArea)
            temp0 = F(n)(0)
            temp1 = F(n)(1)
            temp2 = F(n)(2)
            temp3 = F(n)(3)
            tempAvrg = (alphaAvrg(temp0) + alphaAvrg(temp1) + alphaAvrg(temp2) + alphaAvrg(temp3)) / 4
            'Print(tempAvrg)
            ' tempAvrg2 = ( (90 - alphaAvrg(temp0)) + (90 - alphaAvrg(temp1)) + (90 - alphaAvrg(temp2)) + (90 - alphaAvrg(temp3)) ) / 4
            'Print(tempAvrg2)
            ' tempFSunAcc = tempAvrg * tempArea
            ' tempFSunAcc2 = tempAvrg2 * tempArea
            ' totalSunAcc = totalSunAcc + tempFSunAcc2
            ' tempRad = (SolarConstant - ((SolarConstant / 90) * tempAvrg)) / 1000  'kW / sqm
            tempRad = SolarConstant * Math.Sin((90 - tempAvrg) / (180 / Math.PI)) / 1000
            'Print("    " & math.sin((90 - tempAvrg) / (180 / math.pi)) & "    " & (90 - tempAvrg))
            tempRad = tempRad * tempArea                                                       'kW
            totalSunAcc = totalSunAcc + tempRad                                                 'kW

            For i = 0 To UBound(VecSun)
                'tempAvrg = (alphaAccSunVec(temp0)(i) + alphaAccSunVec(temp1)(i) + alphaAccSunVec(temp2)(i) + alphaAccSunVec(temp3)(i)) / 4
                'tempAvrg = SolarConstant * Math.Sin((90 - tempAvrg) / (180 / Math.PI)) / 1000
                'tempAvrg = tempAvrg * tempArea      'area
                'alphaAccPerWholeMeshAndSunVec(i) = alphaAccPerWholeMeshAndSunVec(i) + tempAvrg
                'Print(alphaAccSunVec(F(n)(0))(i))

                ShadowAccount(i) = ShadowAccount(i) + ((ShadowVertex(temp0)(i) + ShadowVertex(temp1)(i) + ShadowVertex(temp2)(i) + ShadowVertex(temp3)(i)) / 4) * tempArea


            Next
        Next

        For i = 0 To UBound(VecSun)
            ShadowAccount(i) = ShadowAccount(i) / AreaMassProperties.Compute(strMesh).Area
        Next
        '    For i = 0 To UBound(Vecsun)
        '      Print(alphaAccPerWholeMeshAndSunVec(i))   'kWh for every hour (or sun vector respectively)
        '    Next



        'Dim FitArea, meshArea As Double
        'Print(Areamassproperties.compute(strMesh).area & "    val: " & totalSunAcc)
        ' meshArea = Areamassproperties.compute(strMesh).area
        'FitArea = (totalSunAcc / meshArea)
        'Dim Fitness As Double = (1 / 90) * FitArea
        Dim Fitness As Double = totalSunAcc * (UBound(VecSun) + 1)                          'kWh
        'Select Case SunCase
        '    Case "Equinox"
        '        Fitness = Fitness * 2
        'End Select

        'Print(FitArea)
        '      Print("FITNESS in kWh per day: " & Fitness & "      over total area of sqm: " & AreaMassProperties.Compute(strMesh).Area.ToString)
        '    Print("which is in kWh per day per sqm:   " & Fitness / AreaMassProperties.Compute(strMesh).Area)
        '    '______________________________________________________________________________//////////////////////////////////////////////
        '
        '
        '
        '    '	'____________________________________________________________________________________________________________________________
        '    '	'////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        '    '	'////////////////////////////////////////////																VISUALIZATION

        Dim ColMesh As Mesh = New Mesh()
        For i = 0 To UBound(MVert)
            ColMesh.Vertices.Add(MVert(i))
            ColMesh.VertexColors.SetColor(i, c(i))
        Next
        For n = 0 To strMesh.Faces.Count - 1
            ColMesh.Faces.AddFace(F(n)(0), F(n)(1), F(n)(2), F(n)(3))
        Next
        '    '	'______________________________________________________________________________//////////////////////////////////////////////
        '


        SunShadowHeat = New Object(2) {ColMesh, Fitness, ShadowAccount}
    End Function


    Function avgMeshNormal(msh As Mesh) As Vector3d

        Dim i As Integer


        'Dim area(msh.Faces.Count - 1) As Double
        msh.FaceNormals.ComputeFaceNormals()
        Dim area As Double
        Dim vec As Vector3f

        Dim vec3d As Vector3d
        Dim totalWeightedVec As New Vector3d(0, 0, 0)
        For i = 0 To msh.Faces.Count - 1
            Dim mshface As New Mesh()
            mshface.Vertices.Add(msh.Vertices(msh.Faces(i).A))
            mshface.Vertices.Add(msh.Vertices(msh.Faces(i).B))
            mshface.Vertices.Add(msh.Vertices(msh.Faces(i).C))
            mshface.Vertices.Add(msh.Vertices(msh.Faces(i).D))
            mshface.Faces.AddFace(0, 1, 2, 3)
            area = AreaMassProperties.Compute(mshface).Area

            vec = New Vector3f()
            vec = msh.FaceNormals.Item(i)

            vec3d = New Vector3d(vec.X, vec.Y, vec.Z)
            vec3d = Vector3d.Multiply(area, vec3d)
            totalWeightedVec = Vector3d.Add(totalWeightedVec, vec3d)
        Next

        totalWeightedVec.Unitize()
        Return totalWeightedVec


    End Function

    'Public Sub RunScript(ByVal MeshCheckSun As List(Of Mesh), _
    '                     ByVal MeshObstacles As List(Of Mesh), _
    '                     ByVal strSeason As String) 'replace strSeason with list of vectors


    '    '    Dim    meshObj As Guid = doc.Objects.addMesh(MeshCheckSun)
    '    '    doc.Objects.UnselectAll()
    '    '    doc.Objects.Select(meshObj)
    '    '    rhino.RhinoApp.RunScript("_-ReduceMesh _ReductionPercentage " & Convert.toString(5) & " _Enter", False)
    '    '    Dim MObj As  rhino.DocObjects .RhinoObject = Doc.Objects.GetSelectedObjects(False, False).First()
    '    '    doc.Objects.Delete(meshObj, True)
    '    '    ColouredMesh = MObj.Geometry
    '    Dim i, n As Integer
    '    Dim arrMeshObst(MeshObstacles.Count - 1) As Mesh
    '    For i = 0 To MeshObstacles.Count - 1
    '        arrMeshObst(i) = MeshObstacles(i)
    '    Next


    '    Dim _fit As Double = 0
    '    Dim _calc(2) As Object
    '    Dim ColMesh As New List(Of Mesh)
    '    Dim cm As Mesh
    '    Dim _vecs() As Double
    '    Dim __vecs As New List(Of Double)      'should be somehow a matrix
    '    For i = 0 To MeshCheckSun.Count - 1
    '        _calc = SunShadowHeat(MeshCheckSun.Item(i), arrMeshObst, strSeason)
    '        cm = _calc(0)
    '        ColMesh.Add(cm)

    '        _vecs = _calc(2)
    '        For n = 0 To UBound(_vecs)
    '            __vecs.Add(_vecs(n))
    '        Next
    '        _fit = _fit + _calc(1)
    '    Next


    '    '     Print("FITNESS " & _fit)
    '    ColouredMesh = ColMesh
    '    Fitness = _fit

    '    VecsAlphas = __vecs
    'End Sub


    'Function SunShadowHeat(ByVal strMesh As Mesh, ByVal obstacle() As Mesh, ByVal SunCase As String)
    '    Dim i, n As Integer

    '    Const SolarConstant As Double = 1367 * 0.78 'W/sqm....solar constant minus 16% absorbed atmosphere and 6% reflected atmosphere
    '    '____________________________________________________________________________________________________________________________
    '    '////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '    '////////////////////////////////////////////																	SUN VECTORS
    '    Dim VecSun() As Vector3d
    '    Dim VecFaceAlpha() As Double


    '    Select Case SunCase
    '        Case "Summer_Solstice"
    '            ReDim VecSun(15)
    '            VecSun(0).X = -0.851408 : VecSun(0).Y = -0.520828 : VecSun(0).Z = -0.061981    '21st June 5:00
    '            VecSun(1).X = -0.910902 : VecSun(1).Y = -0.349856 : VecSun(1).Z = -0.218767    '21st June 6:00
    '            VecSun(2).X = -0.908337 : VecSun(2).Y = -0.17336 : VecSun(2).Z = -0.380617     '21st June 7:00
    '            VecSun(3).X = -0.84389 : VecSun(3).Y = -0.003366 : VecSun(3).Z = -0.536506       '21st June 8:00
    '            VecSun(4).X = -0.72195 : VecSun(4).Y = 0.148546 : VecSun(4).Z = -0.675812     '21st June 9:00
    '            VecSun(5).X = -0.550826 : VecSun(5).Y = 0.272026 : VecSun(5).Z = -0.789045   '21st June 10:00
    '            VecSun(6).X = -0.342176 : VecSun(6).Y = 0.358661 : VecSun(6).Z = -0.868492      '21st June 11:00
    '            VecSun(7).X = -0.110214 : VecSun(7).Y = 0.402549 : VecSun(7).Z = -0.908739    '21st June 12:00
    '            VecSun(8).X = 0.129257 : VecSun(8).Y = 0.4007 : VecSun(8).Z = -0.907046    '21st June 13:00
    '            VecSun(9).X = 0.359921 : VecSun(9).Y = 0.353241 : VecSun(9).Z = -0.863526    '21st June 14:00
    '            VecSun(10).X = 0.566064 : VecSun(10).Y = 0.263404 : VecSun(10).Z = -0.781146    '21st June 15:00
    '            VecSun(11).X = 0.733643 : VecSun(11).Y = 0.13731 : VecSun(11).Z = -0.665518    '21st June 16:00
    '            VecSun(12).X = 0.85124 : VecSun(12).Y = -0.016451 : VecSun(12).Z = -0.524519    '21st June 17:00
    '            VecSun(13).X = 0.910844 : VecSun(13).Y = -0.187403 : VecSun(13).Z = -0.367755   '21st June 18:00
    '            VecSun(14).X = 0.908394 : VecSun(14).Y = -0.3639 : VecSun(14).Z = -0.205906    '21st June 19:00
    '            VecSun(15).X = 0.844057 : VecSun(15).Y = -0.533917 : VecSun(15).Z = -0.049998    '21st June 20:00

    '        Case "Winter_Solstice"
    '            ReDim VecSun(7)
    '            VecSun(0).X = -0.713623 : VecSun(0).Y = 0.693832 : VecSun(0).Z = -0.096647    '22st december 9:00
    '            VecSun(1).X = -0.540114 : VecSun(1).Y = 0.815441 : VecSun(1).Z = -0.208164  '22st december 10:00
    '            VecSun(2).X = -0.329823 : VecSun(2).Y = 0.899823 : VecSun(2).Z = -0.285544      '22st december 11:00
    '            VecSun(3).X = -0.09707 : VecSun(3).Y = 0.94123 : VecSun(3).Z = -0.323517      '22st december 12:00
    '            VecSun(4).X = 0.142294 : VecSun(4).Y = 0.936843 : VecSun(4).Z = -0.319496     '22st december 13:00
    '            VecSun(5).X = 0.371968 : VecSun(5).Y = 0.88696 : VecSun(5).Z = -0.273755     '22st december 14:00
    '            VecSun(6).X = 0.57631 : VecSun(6).Y = 0.794978 : VecSun(6).Z = -0.18941     '22st december 15:00
    '            VecSun(7).X = 0.741405 : VecSun(7).Y = 0.667162 : VecSun(7).Z = -0.072204     '22st december 16:00

    '            '        VecSun(0).X = 0.142294:VecSun(0).Y = 0.936843:VecSun(0).Z = -0.319496     '22st december 13:00
    '        Case "Equinox"
    '            ReDim VecSun(11)
    '            VecSun(0).X = -0.993308 : VecSun(0).Y = 0.088287 : VecSun(0).Z = -0.074462 '20st March 7:00
    '            VecSun(1).X = -0.929577 : VecSun(1).Y = 0.274713 : VecSun(1).Z = -0.245805 '20st March 8:00
    '            VecSun(2).X = -0.80247 : VecSun(2).Y = 0.442617 : VecSun(2).Z = -0.400165 '20st March 9:00
    '            VecSun(3).X = -0.620654 : VecSun(3).Y = 0.580541 : VecSun(3).Z = -0.527031 '20st March 10:00
    '            VecSun(4).X = -0.396523 : VecSun(4).Y = 0.679066 : VecSun(4).Z = -0.617769 '20st March 11:00
    '            VecSun(5).X = -0.145358 : VecSun(5).Y = 0.731463 : VecSun(5).Z = -0.666208   '20st March 12:00
    '            VecSun(6).X = 0.115717 : VecSun(6).Y = 0.734146 : VecSun(6).Z = -0.669058   '20st March 13:00
    '            VecSun(7).X = 0.368903 : VecSun(7).Y = 0.686919 : VecSun(7).Z = -0.626141    '20st March 14:00
    '            VecSun(8).X = 0.59694 : VecSun(8).Y = 0.592988 : VecSun(8).Z = -0.540396    '20st March 15:00
    '            VecSun(9).X = 0.784279 : VecSun(9).Y = 0.458745 : VecSun(9).Z = -0.417683    '20st March 16:00
    '            VecSun(10).X = 0.91815 : VecSun(10).Y = 0.293326 : VecSun(10).Z = -0.266384   '20st March 17:00
    '            VecSun(11).X = 0.989425 : VecSun(11).Y = 0.107998 : VecSun(11).Z = -0.096827    '20st March 18:00

    '    End Select
    '    For i = 0 To UBound(VecSun)
    '        VecSun(i).Reverse()
    '    Next
    '    ReDim VecFaceAlpha(UBound(VecSun))
    '    '______________________________________________________________________________//////////////////////////////////////////////



    '    '____________________________________________________________________________________________________________________________
    '    '////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '    '////////////////////////////////////////////														CALCULATING SUN+SHADOW

    '    Dim cen(strMesh.Faces.Count) As Point3d
    '    Dim F(strMesh.Faces.Count - 1)() As Integer
    '    '    Dim bool As Boolean
    '    '    Dim temp1, temp2, temp3, temp4 As point3f
    '    '    Dim temparr(3) As point3f
    '    For i = 0 To strMesh.Faces.Count - 1
    '        cen(i) = strMesh.Faces.GetFaceCenter(i)
    '        F(i) = New Integer(3) {}
    '        F(i)(0) = strMesh.Faces.GetFace(i).A
    '        F(i)(1) = strMesh.Faces.GetFace(i).B
    '        F(i)(2) = strMesh.Faces.GetFace(i).C
    '        F(i)(3) = strMesh.Faces.GetFace(i).D
    '        'Print(F(i)(0) & " " & F(i)(1) & " " & F(i)(2) & " " & F(i)(3))
    '        '      bool = strMesh.Faces.GetFaceVertices(i, temp1, temp2, temp3, temp4)
    '        '      temparr(0) = temp1
    '        '      temparr(1) = temp2
    '        '      temparr(2) = temp3
    '        '      temparr(3) = temp4
    '        '      F(i) = temparr
    '        '      'Print(MVert(i)(1).X)
    '    Next

    '    Dim MVertNorm(strMesh.Vertices.Count - 1) As Vector3f
    '    Dim MVert(strMesh.Vertices.Count - 1) As Point3f
    '    ' Dim tempv2 As Point3f
    '    ' Dim tempv As vector3f
    '    For i = 0 To strMesh.Vertices.Count - 1
    '        '   tempv = strMesh.normals.item(i)
    '        MVertNorm(i) = strMesh.Normals.Item(i)
    '        '  tempv2 = strMesh.Vertices.item(i)
    '        MVert(i) = strMesh.Vertices.Item(i)
    '        ' Print(MVert(i).X & " " & MVert(i).Y & " " & MVert(i).Z)
    '        ' Print(tempv.X & " " & tempv.Y & " " & tempv.Z)
    '    Next





    '    Dim arrSunPStart, arrOffsetP As Point3d
    '    Dim VecOff As Vector3f
    '    Dim IntersectCheck, IntC2 As Double
    '    '    Dim intShadowAccount(UBound(MVert)) As Integer
    '    Dim kkk As Integer = 0
    '    Dim raySunCrvs As Ray3d
    '    Dim alpha As Double
    '    Dim alphaAcc(UBound(MVert)), alphaAvrg(UBound(MVert)) As Double
    '    Dim R, G, B As Double
    '    Dim c(UBound(MVert)) As Color

    '    Dim alphaAccSunVec(UBound(MVert))() As Double

    '    For i = 0 To UBound(MVert)
    '        arrSunPStart = New Point3d(MVert(i).X, MVert(i).Y, MVert(i).Z)
    '        VecOff = MVertNorm(i)
    '        VecOff.Unitize()
    '        VecOff = Vector3f.Multiply(VecOff, 0.1)
    '        arrOffsetP = Point3d.Add(VecOff, MVert(i))
    '        'Print(MVertNorm(i).Length & " " & VecOff.Length)

    '        alphaAccSunVec(i) = New Double(UBound(VecSun)) {}
    '        For n = 0 To UBound(VecSun)

    '            raySunCrvs = New Ray3d(arrOffsetP, VecSun(n))
    '            'Print(raySunCrvs.Direction.ToString)
    '            IntersectCheck = Intersect.Intersection.MeshRay(strMesh, raySunCrvs)      'checks self-blocking to sun (like an overhang or so)
    '            'Print(IntersectCheck)
    '            For kkk = 0 To UBound(obstacle)
    '                IntC2 = Intersect.Intersection.MeshRay(obstacle(kkk), raySunCrvs)
    '                If IntC2 >= 0 Then
    '                    kkk = UBound(obstacle)
    '                End If
    '            Next

    '            If IntersectCheck < 0 And IntC2 < 0 Then
    '                '          intShadowAccount(i) = intShadowAccount(i) + 1
    '                alpha = Vector3d.VectorAngle(MVertNorm(i), VecSun(n))
    '                alpha = alpha * 180 / Math.PI
    '                ' Print("oho " & alpha)
    '                If alpha > 90 Then
    '                    alphaAcc(i) = alphaAcc(i) + 90
    '                    alpha = 90
    '                Else
    '                    alphaAcc(i) = alphaAcc(i) + alpha
    '                End If
    '            Else
    '                alphaAcc(i) = alphaAcc(i) + 90
    '                alpha = 90
    '            End If

    '            alphaAccSunVec(i)(n) = alpha
    '        Next

    '        '      For n = 0 To UBound(VecSun)
    '        '        Print(n & ": " & alphaAccSunVec(i)(n))
    '        '      next



    '        alphaAvrg(i) = alphaAcc(i) / (UBound(VecSun) + 1)

    '        If alphaAvrg(i) > 45 And alphaAvrg(i) <= 67.5 Then
    '            R = 255
    '            G = 255 - ((alphaAvrg(i) - 45) * (255 / 22.5))
    '            B = 0
    '            'Print(alphaAvrg(i) & "     R: " & R & "   G: " & G & "    B: " & B)
    '        ElseIf alphaAvrg(i) > 67.5 Then
    '            R = 255 - ((alphaAvrg(i) - 67.5) * (255 / 22.5))
    '            G = 0
    '            B = ((alphaAvrg(i) - 67.5) * (255 / 22.5))
    '            ' Print(alphaAvrg(i) & "     R: " & R & "   G: " & G & "    B: " & B)
    '        Else
    '            R = 255
    '            G = 255
    '            B = 0
    '            ' Print(alphaAvrg(i) & "     R: " & R & "   G: " & G & "    B: " & B)
    '        End If



    '        '      Print("R " & int(R) & " G " & int(G) & " B " & int(B))
    '        '      Print(alphaAvrg(i))

    '        c(i) = Color.FromArgb(Int(R), Int(G), Int(B))
    '        'Print(c(i).R.ToString & " " & c(i).G.ToString & " " & c(i).B.ToString)
    '        'Print(alphaacc(i))
    '    Next


    '    '    '______________________________________________________________________________//////////////////////////////////////////////






    '    '    '____________________________________________________________________________________________________________________________
    '    '    '////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '    '    '////////////////////////////////////////////																		FITNESS
    '    '    Dim tempMesh, tempArea, tempAvrg, temp0, temp1, temp2, temp3
    '    '    Dim tempFSunAcc, totalSunAcc
    '    '
    '    '
    '    '    Dim tempAvrg2, tempFSunAcc2
    '    '
    '    '

    '    Dim tempArea As Double
    '    Dim tempMesh As Mesh
    '    Dim tempAvrg, tempAvrg2, tempFSunAcc, tempFSunAcc2, totalSunAcc, tempRad As Double
    '    Dim temp0, temp1, temp2, temp3 As Integer
    '    Dim alphaAccPerWholeMeshAndSunVec(UBound(VecSun)) As Double

    '    For n = 0 To strMesh.Faces.Count - 1
    '        tempMesh = New Mesh()
    '        tempMesh.Vertices.Add(MVert(F(n)(0)))
    '        tempMesh.Vertices.Add(MVert(F(n)(1)))
    '        tempMesh.Vertices.Add(MVert(F(n)(2)))
    '        tempMesh.Vertices.Add(MVert(F(n)(3)))
    '        tempMesh.Faces.AddFace(0, 1, 2, 3)
    '        tempArea = AreaMassProperties.Compute(tempMesh).Area
    '        'Print("Area ni sqm:   " & tempArea)
    '        temp0 = F(n)(0)
    '        temp1 = F(n)(1)
    '        temp2 = F(n)(2)
    '        temp3 = F(n)(3)
    '        tempAvrg = (alphaAvrg(temp0) + alphaAvrg(temp1) + alphaAvrg(temp2) + alphaAvrg(temp3)) / 4
    '        'Print(tempAvrg)
    '        ' tempAvrg2 = ( (90 - alphaAvrg(temp0)) + (90 - alphaAvrg(temp1)) + (90 - alphaAvrg(temp2)) + (90 - alphaAvrg(temp3)) ) / 4
    '        'Print(tempAvrg2)
    '        ' tempFSunAcc = tempAvrg * tempArea
    '        ' tempFSunAcc2 = tempAvrg2 * tempArea
    '        ' totalSunAcc = totalSunAcc + tempFSunAcc2
    '        ' tempRad = (SolarConstant - ((SolarConstant / 90) * tempAvrg)) / 1000  'kW / sqm
    '        tempRad = SolarConstant * Math.Sin((90 - tempAvrg) / (180 / Math.PI)) / 1000
    '        'Print("    " & math.sin((90 - tempAvrg) / (180 / math.pi)) & "    " & (90 - tempAvrg))
    '        tempRad = tempRad * tempArea                                                       'kW
    '        totalSunAcc = totalSunAcc + tempRad                                                 'kW

    '        For i = 0 To UBound(VecSun)
    '            tempAvrg = (alphaAccSunVec(temp0)(i) + alphaAccSunVec(temp1)(i) + alphaAccSunVec(temp2)(i) + alphaAccSunVec(temp3)(i)) / 4
    '            tempAvrg = SolarConstant * Math.Sin((90 - tempAvrg) / (180 / Math.PI)) / 1000
    '            tempAvrg = tempAvrg * tempArea
    '            alphaAccPerWholeMeshAndSunVec(i) = alphaAccPerWholeMeshAndSunVec(i) + tempAvrg

    '            'Print(alphaAccSunVec(F(n)(0))(i))

    '        Next
    '    Next

    '    '    For i = 0 To UBound(Vecsun)
    '    '      Print(alphaAccPerWholeMeshAndSunVec(i))   'kWh for every hour (or sun vector respectively)
    '    '    Next



    '    'Dim FitArea, meshArea As Double
    '    'Print(Areamassproperties.compute(strMesh).area & "    val: " & totalSunAcc)
    '    ' meshArea = Areamassproperties.compute(strMesh).area
    '    'FitArea = (totalSunAcc / meshArea)
    '    'Dim Fitness As Double = (1 / 90) * FitArea
    '    Dim Fitness As Double = totalSunAcc * (UBound(VecSun) + 1)                          'kWh
    '    Select Case SunCase
    '        Case "Equinox"
    '            Fitness = Fitness * 2
    '    End Select

    '    'Print(FitArea)
    '    '      Print("FITNESS in kWh per day: " & Fitness & "      over total area of sqm: " & AreaMassProperties.Compute(strMesh).Area.ToString)
    '    '    Print("which is in kWh per day per sqm:   " & Fitness / AreaMassProperties.Compute(strMesh).Area)
    '    '    '______________________________________________________________________________//////////////////////////////////////////////
    '    '
    '    '
    '    '
    '    '    '	'____________________________________________________________________________________________________________________________
    '    '    '	'////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    '    '    '	'////////////////////////////////////////////																VISUALIZATION

    '    Dim ColMesh As Mesh = New Mesh()
    '    For i = 0 To UBound(MVert)
    '        ColMesh.Vertices.Add(MVert(i))
    '        ColMesh.VertexColors.SetColor(i, c(i))
    '    Next
    '    For n = 0 To strMesh.Faces.Count - 1
    '        ColMesh.Faces.AddFace(F(n)(0), F(n)(1), F(n)(2), F(n)(3))


    '    Next
    '    '    Dim ColMesh
    '    '    ColMesh = rhino.AddMesh(Mvert, F, , , c)
    '    '    Call rhino.EnableRedraw(True)
    '    '    Call rhino.SelectObject(colmesh)
    '    '    Call rhino.ZoomSelected()
    '    '    rhino.UnselectAllObjects()
    '    '    'Call rhino.EnableRedraw(False)
    '    '    '	'______________________________________________________________________________//////////////////////////////////////////////
    '    '


    '    SunShadowHeat = New Object(2) {ColMesh, Fitness, alphaAccPerWholeMeshAndSunVec}
    '    '    Return strMesh
    'End Function

End Class
