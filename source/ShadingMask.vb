Imports Grasshopper.Kernel
Imports Rhino.Geometry
Imports System.Drawing

Imports Rhino.RhinoDoc
'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
'!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
' TO DO: Add feature: reflectivity (optional), set bounces. need to defince surface reflectance of obstacles then
' This is for DIFFUSE. do the same for DIRECT
Public Class ShadingMask
    Public ReductionFactor As Double
    Public domeOut As Mesh

    '///////////////////////////////////////////////////////////////////////////////////////////
    'create a SkyDome, with its center being center of the mesh, and large enough that all obstacles are within
    '///////////////////////////////////////////////////////////////////////////////////////////
    Public Sub New(ByRef panel As Mesh, ByRef obst As List(Of Mesh), _
                   ByRef intResolution As Integer, ByRef blnSimple As Boolean)
        Const rad = Math.PI / 180

        '///////////////////////////////////////////////////////////////////////////////////////////
        '1.     bounding box obstacles and panel
        Dim allobjs As New Mesh()   'all objects - obstructions and panel
        allobjs.Append(panel)
        For Each obstI As Mesh In obst
            allobjs.Append(obstI)
        Next
        Dim bb As Rhino.Geometry.BoundingBox = allobjs.GetBoundingBox(False)    'bounding box for all objects
        '///////////////////////////////////////////////////////////////////////////////////////////



        '///////////////////////////////////////////////////////////////////////////////////////////
        '2.     creating SkyDome-Sphere
        Dim panelCen As Point3d = panel.GetBoundingBox(False).Center    'panel center
        '!!! actually, center-z should be at the lowest z-point of all mesh vertices...? no not necessarily

        Dim maxDist As Double = 0
        Dim evalDist As Double
        For Each p As Point3d In bb.GetCorners()
            evalDist = panelCen.DistanceTo(p)
            If evalDist > maxDist Then          'finding the furthest BB cornerpoint to panelCen
                maxDist = evalDist
            End If
        Next
        Dim domeSphere As Sphere = New Sphere(panelCen, maxDist)
        '///////////////////////////////////////////////////////////////////////////////////////////


        '///////////////////////////////////////////////////////////////////////////////////////////
        '3.     discretizing the SkyDome-Sphere (meshing)
        Dim domemeshparam As MeshingParameters
        domemeshparam = MeshingParameters.Minimal           'minimal mesh settings
        domemeshparam.GridAmplification = intResolution     'adjusting mesh. higher intResolution gives higher resolution of SkyDome Mesh
        Dim domemeshes = Mesh.CreateFromBrep(domeSphere.ToBrep, domemeshparam)
        Dim brepmesh = New Mesh()
        For Each facemesh As Mesh In domemeshes
            brepmesh.Append(facemesh)
        Next
        '///////////////////////////////////////////////////////////////////////////////////////////


        '///////////////////////////////////////////////////////////////////////////////////////////
        '4.     creating Dome from SkyDome-Sphere. with z>= panel 
        Dim dome = New Mesh()                               'SkyDome
        Dim domePatchCen As New List(Of Point3d)            'center of a patch
        Dim domePatchArea As New List(Of Double)            'area of a patch
        For i As Integer = 0 To brepmesh.Faces.Count - 1
            Dim mshface As New Mesh()
            If brepmesh.Faces(i).IsQuad Then
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).A))
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).B))
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).C))
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).D))
                mshface.Faces.AddFace(0, 1, 2, 3)
            Else
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).A))
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).B))
                mshface.Vertices.Add(brepmesh.Vertices(brepmesh.Faces(i).C))
                mshface.Faces.AddFace(0, 1, 2)
            End If

            ' ignoring all patches of the resulting skydome with lower z-value then panelCen z-coordinate
            Dim p3d As Point3d = AreaMassProperties.Compute(mshface).Centroid
            If p3d.Z >= panelCen.Z Then
                domePatchCen.Add(p3d) '!!!!!!!!!!!!!critical: what about a flying object in the clouds. it should receive diffuse radiation from all directions
                domePatchArea.Add(AreaMassProperties.Compute(mshface).Area)
                dome.Append(mshface)
            End If
        Next
        '///////////////////////////////////////////////////////////////////////////////////////////


        ''///////////////////////////////////////////////////////////////////////////////////////////
        ''5.      shoot rays from each mesh vertex of PVmodule to meshcenters of skydome
        'Dim domePatchBln As New List(Of Boolean) 'true means access. false means no radiation  
        'For i As Integer = 0 To domePatchCen.Count - 1
        '    domePatchBln.Add(False)
        '    Dim vec As New Vector3d(domePatchCen(i))
        '    vec = Vector3d.Subtract(vec, New Vector3d(mshCen))
        '    'check if this vec is not pointing "behind" the mesh.
        '    Dim shfc As New ShadowFactor()
        '    Dim mshNormal As New Vector3d()
        '    mshNormal = shfc.avgMeshNormal(msh)
        '    Dim vecangl As Double = Vector3d.VectorAngle(mshNormal, vec)
        '    If vecangl / rad <= 90 Then
        '        domePatchBln(i) = True
        '        Dim rayDome = New Ray3d(mshCen, vec)
        '        For k As Integer = 0 To obst.Count - 1
        '            Dim IntersectCheck As Double = Intersect.Intersection.MeshRay(obst(k), rayDome)      'checks self-blocking to sun (like an overhang or so)
        '            If IntersectCheck >= 0 Then
        '                domePatchBln(i) = False
        '                Exit For
        '            End If
        '        Next
        '    End If
        'Next
        ''///////////////////////////////////////////////////////////////////////////////////////////


        '///////////////////////////////////////////////////////////////////////////////////////////
        'two different modes to calculate shading mask: simple (blnSimple=true) or detailed (blnSimple=false)
        '///////////////////////////////////////////////////////////////////////////////////////////
        If blnSimple = True Then
            '///////////////////////////////////////////////////////////////////////////////////////////
            '5.a)      shoot rays from avg mesh vertex of panel to each mesh vertex of skydome
            '           make list, where all skydome patches are assumed to be blocked. Only if a ray reaches that patch, ...
            '           it will get a positive factor (true) for diffuse radiation.
            '           So actually it starts with factor 0 (DHI*0=0), and only if rays hit patches this factor ...
            '           can grow proportionally to the area of that patch
            Dim MVert(dome.Vertices.Count - 1) As Point3f   'all vertices of SkyDome
            For i = 0 To dome.Vertices.Count - 1
                MVert(i) = dome.Vertices.Item(i)
            Next
            Dim domePatchBln As New List(Of Boolean)        'true means access. false means no radiation  
            Dim PanelShadingFac As New ShadowFactor()       'this is the panel to be evaluated. 
            Dim avgPanelMshNormal As New Vector3d()         'avg normal of Panel
            avgPanelMshNormal = PanelShadingFac.avgMeshNormal(panel)    'calculating average mesh normal
            For i As Integer = 0 To UBound(MVert)
                domePatchBln.Add(False)
                Dim vec As New Vector3d(MVert(i))
                vec = Vector3d.Subtract(vec, New Vector3d(panelCen))    'vector between panel Center and SkyDome vertex

                '       account for angle of PVmodule, so that everything "behind" the module doesnt need to be checked.
                '       check if this vec is not pointing "behind" the mesh.
                Dim vecangl As Double = Vector3d.VectorAngle(avgPanelMshNormal, vec)
                If vecangl / rad <= 90 Then                 'if angle between panel normal and mesh vertex >= 90 it means, skydome patch is "behind" panel
                    domePatchBln(i) = True                  'it's not behind, so True (radiation access)
                    Dim rayDome = New Ray3d(panelCen, vec)
                    For k As Integer = 0 To obst.Count - 1
                        Dim IntersectCheck As Double = Intersect.Intersection.MeshRay(obst(k), rayDome)      'checks blocking to sun by obstruction
                        If IntersectCheck >= 0 Then
                            domePatchBln(i) = False
                            Exit For
                        End If
                    Next
                End If
            Next
            Dim domeTotArea As Double = AreaMassProperties.Compute(dome).Area
            Dim weightedFactor As Double = 0
            'calculate how much of the SkyDome is blocked... 
            'Go through all patches and check, how many vertices of each face are blocked
            For i As Integer = 0 To dome.Faces.Count - 1
                Dim count As Double = 0
                If dome.Faces(i).IsQuad Then
                    If domePatchBln(dome.Faces(i).A) = True Then count = count + 1
                    If domePatchBln(dome.Faces(i).B) = True Then count = count + 1
                    If domePatchBln(dome.Faces(i).C) = True Then count = count + 1
                    If domePatchBln(dome.Faces(i).D) = True Then count = count + 1
                    count = count / 4
                Else
                    If domePatchBln(dome.Faces(i).A) = True Then count = count + 1
                    If domePatchBln(dome.Faces(i).B) = True Then count = count + 1
                    If domePatchBln(dome.Faces(i).C) = True Then count = count + 1
                    count = count / 3
                End If
                weightedFactor = weightedFactor + (count * domePatchArea(i))
            Next
            'OUTPUTTING REDUCTION FACTOR
            ReductionFactor = weightedFactor / domeTotArea      'number 0-1. 1 = full acces, 0 = no access

            'VISUALIZATION - color vertices
            Dim c As Color
            For i = 0 To dome.Vertices.Count - 1
                If domePatchBln(i) = False Then
                    c = Color.Black
                Else
                    c = Color.White
                End If
                dome.VertexColors.SetColor(i, c)
            Next

            '///////////////////////////////////////////////////////////////////////////////////////////
        Else
            '///////////////////////////////////////////////////////////////////////////////////////////
            '5.b)     shading mask for each panel mesh face. 
            '           Shoot rays from each mesh vertex of panel to each mesh vertex of skydome.
            '           Multiply each shadingmask-factor with respective panel mesh-face-area...
            '           and divide by total panel mesh-face-area.
            '           Make list, where all skydome patches are assumed to be blocked. Only if a ray reaches that patch, ...
            '           it will get a positive factor for diffuse radiation.
            '           So actually it starts with factor 0 (DHI*0=0), and only if rays hit patches this factor ...
            '           can grow proportionally to the area of that patch

            Dim panelPatchCen As New List(Of Point3d)            'list of centers of faces
            Dim panelPatchArea As New List(Of Double)            'list of areas of faces
            Dim panelNormals As New List(Of Vector3f)            'list of face normals
            panel.FaceNormals.ComputeFaceNormals()
            For i As Integer = 0 To panel.Faces.Count - 1
                Dim mshface As New Mesh()
                mshface.Vertices.Add(panel.Vertices(panel.Faces(i).A))
                mshface.Vertices.Add(panel.Vertices(panel.Faces(i).B))
                mshface.Vertices.Add(panel.Vertices(panel.Faces(i).C))
                mshface.Vertices.Add(panel.Vertices(panel.Faces(i).D))
                mshface.Faces.AddFace(0, 1, 2, 3)

                panelNormals.Add(panel.FaceNormals.Item(i))
                panelPatchCen.Add(AreaMassProperties.Compute(mshface).Centroid)
                panelPatchArea.Add(AreaMassProperties.Compute(mshface).Area)
            Next

            Dim factors(panelNormals.Count - 1) As Double           'shading factors for each patch
            Dim TotalFactors As Double = 0                          'summing up all factors and weighting them with area of patch
            Dim TotalPanelArea As Double = AreaMassProperties.Compute(panel).Area
            Dim calcFactor As New Object                            'object containing (0) shadingfactor for all patches and (1) 2d-list of boolean saying if patch-vertices are blocked or not 
            Dim ArrdomePatchBln()() As Boolean = New Boolean(panelNormals.Count - 1)() {}
            For u As Integer = 0 To panelNormals.Count - 1
                'function, returning shading factor
                calcFactor = makeShadingMask(dome, panelCen, panelPatchCen(u), panelNormals(u), obst)
                factors(u) = calcFactor(0)
                factors(u) = factors(u) * panelPatchArea(u)
                TotalFactors = TotalFactors + factors(u)

                ArrdomePatchBln(u) = New Boolean() {}
                ArrdomePatchBln(u) = calcFactor(1)                  'booleans for each vertex of this patch
            Next
            'OUTPUTTING REDUCTION FACTOR
            ReductionFactor = TotalFactors / TotalPanelArea      'number 0-1. 1 = full acces, 0 = no access

            Dim domePatchBln As Object
            domePatchBln = ArrdomePatchBln(0)

            'VISUALIZATION - color vertices
            'AVERAGE OF ALL domePatchBlns
            Dim c As Color
            Dim R, G, B As Double
            Dim TrueCount As Double
            For i = 0 To dome.Vertices.Count - 1
                'calculating average blockage per skydome vertex
                TrueCount = 0
                For u As Integer = 0 To UBound(ArrdomePatchBln)
                    If ArrdomePatchBln(u)(i) = True Then
                        TrueCount = TrueCount + 1
                    End If
                Next
                R = (255 / (UBound(ArrdomePatchBln) + 1)) * TrueCount
                G = R
                B = R
                c = Color.FromArgb(Int(R), Int(G), Int(B))
                dome.VertexColors.SetColor(i, c)
            Next
            '///////////////////////////////////////////////////////////////////////////////////////////
        End If

        'Dim mat As Rhino.Display.DisplayMaterial = New Rhino.Display.DisplayMaterial()
        'mat.Transparency = 0.4

        'Dim mshdraw As Rhino.Display = New Rhino.Display.draw
        'mshdraw.DrawMeshShaded(dome, mat)

        '///////////////////////////////////////////////////////////////////////////////////////////
        'outputting coloured dome
        domeOut = dome
        '///////////////////////////////////////////////////////////////////////////////////////////




    End Sub


    '///////////////////////////////////////////////////////////////////////////////////////////
    'calculating shading mask per panel mesh face
    Private Function makeShadingMask(ByRef dome As Mesh, ByRef panelCen As Point3d, _
                                     ByRef panelPatchCen As Point3d, ByRef panelNormals As Vector3f, _
                                     ByRef obst As List(Of Mesh))

        Const rad = Math.PI / 180
        Dim shadingFac As Double = 0
        '///////////////////////////////////////////////////////////////////////////////////////////
        Dim MVert(dome.Vertices.Count - 1) As Point3f
        For i = 0 To dome.Vertices.Count - 1
            MVert(i) = dome.Vertices.Item(i)
        Next

        Dim domePatchBln(UBound(MVert)) As Boolean 'true means access. false means no radiation  

        For i As Integer = 0 To UBound(MVert)
            domePatchBln(i) = False
            Dim vec As New Vector3d(MVert(i))
            vec = Vector3d.Subtract(vec, New Vector3d(panelPatchCen))

            '       account for angle of PVmodule, so that everything "behind" the module doesnt need to be checked.
            'check if this vec is not pointing "behind" the mesh.
            Dim vecangl As Double = Vector3d.VectorAngle(panelNormals, vec)
            If vecangl / rad <= 90 Then
                domePatchBln(i) = True
                Dim rayDome = New Ray3d(panelCen, vec)
                For k As Integer = 0 To obst.Count - 1
                    Dim IntersectCheck As Double = Intersect.Intersection.MeshRay(obst(k), rayDome)      'checks self-blocking to sun (like an overhang or so)
                    If IntersectCheck >= 0 Then
                        domePatchBln(i) = False
                        Exit For
                    End If
                Next
            End If
        Next



        '///////////////////////////////////////////////////////////////////////////////////////////
        'go through faces of halfdome and check, how many vertices are blocked or not (blnpatchblocked or sth)
        'differentiate between quads and triangles
        'make reduction factor according to number of blocked vertices and area of that mesh face / patch
        Dim TotalHalfDomeArea As Double = AreaMassProperties.Compute(dome).Area
        Dim AccessHalfDomeArea As Double = 0
        For i = 0 To dome.Faces.Count - 1
            Dim countAccess As Integer = 0
            Dim tempMesh As New Mesh()
            Dim tempArea As Double
            Dim accessFactor As Double
            If dome.Faces(i).IsQuad Then
                If domePatchBln(dome.Faces(i).A) = True Then countAccess = countAccess + 1
                If domePatchBln(dome.Faces(i).B) = True Then countAccess = countAccess + 1
                If domePatchBln(dome.Faces(i).C) = True Then countAccess = countAccess + 1
                If domePatchBln(dome.Faces(i).D) = True Then countAccess = countAccess + 1

                tempMesh.Vertices.Add(MVert(dome.Faces(i).A))
                tempMesh.Vertices.Add(MVert(dome.Faces(i).B))
                tempMesh.Vertices.Add(MVert(dome.Faces(i).C))
                tempMesh.Vertices.Add(MVert(dome.Faces(i).D))
                tempMesh.Faces.AddFace(0, 1, 2, 3)
                accessFactor = countAccess / 4
            Else
                If domePatchBln(dome.Faces(i).A) = True Then countAccess = countAccess + 1
                If domePatchBln(dome.Faces(i).B) = True Then countAccess = countAccess + 1
                If domePatchBln(dome.Faces(i).C) = True Then countAccess = countAccess + 1

                tempMesh.Vertices.Add(MVert(dome.Faces(i).A))
                tempMesh.Vertices.Add(MVert(dome.Faces(i).B))
                tempMesh.Vertices.Add(MVert(dome.Faces(i).C))
                tempMesh.Faces.AddFace(0, 1, 2)
                accessFactor = countAccess / 3
            End If
            tempArea = AreaMassProperties.Compute(tempMesh).Area
            AccessHalfDomeArea = AccessHalfDomeArea + (tempArea * accessFactor)
        Next
        '///////////////////////////////////////////////////////////////////////////////////////////


        '///////////////////////////////////////////////////////////////////////////////////////////
        '3      if part of dome is blocked, then take that area of the dome meshface and use it as reduction factor
        shadingFac = AccessHalfDomeArea / TotalHalfDomeArea    '1 is no reduction. 0 is full reduction. 0.5 is half reduction
        '///////////////////////////////////////////////////////////////////////////////////////////


        makeShadingMask = New Object(1) {shadingFac, domePatchBln}
    End Function
End Class
