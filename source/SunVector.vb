


Public Class SunVector
    Const pi = Math.PI
    Const twopi = 2 * pi
    Const rad = pi / 180
    Const dEarthMeanRadius = 6371.01        ' in km
    Const dAstronomicalUnit = 149597890     ' in km


    Public udtTime As cTime
    Public udtLocation As cLocation
    Public udtCoordinates As cSunCoordinates
    Public udtCoordXYZ As cSunXYZ



    '//////////////////////////////////////////////////////////////////////////
    '//////////////////////////////////////////////////////////////////////////

    Public Sub New(_year As Integer, _month As Integer, _day As Integer, _
                   _hours As Double, _minutes As Double, _seconds As Double, _
                   _longitude As Double, _latitude As Double)
        udtTime.iYear = _year
        udtTime.iMonth = _month
        udtTime.iDay = _day
        udtTime.dHours = _hours
        udtTime.dMinutes = _minutes
        udtTime.dSeconds = _seconds
        udtLocation.dLatitude = _latitude
        udtLocation.dLongitude = _longitude

        udtCoordinates = sunpos()
        ' Call MsgBox("azimuth: " & CStr(udtCoordinates.dAzimuth) & vbCrLf & _
        '             "Zenith: " & CStr(udtCoordinates.dZenithAngle))
        udtCoordXYZ = SunposXYZ()
    End Sub



    'PSA position sun algorithm
    'http://www.psa.es/sdg/sunpos.htm
    'http://www.sciencedirect.com/science/article/pii/S0038092X00001560
    Function sunpos() As cSunCoordinates
        'Main variables
        Dim dElapsedJulianDays As Double
        Dim dDecimalHours As Double
        Dim dEclipticLongitude As Double
        Dim dEclipticObliquity As Double
        Dim dRightAscension As Double
        Dim dDeclination As Double

        'Auxuliary variables
        Dim dY As Double
        Dim dX As Double

        'Calculate difference in days between the current Julian Day
        'and JD 2451545.0, which is noon 1 January 2000 Universal Time
        Dim dJulianDate As Double
        Dim liAux1 As Long
        Dim liAux2 As Long
        'Calculate time of the day in UT decimal hours
        dDecimalHours = udtTime.dHours + (udtTime.dMinutes + udtTime.dSeconds / 60.0) / 60.0
        'Calculate current Julian Day
        liAux1 = (udtTime.iMonth - 14) / 12
        liAux2 = (1461 * (udtTime.iYear + 4800 + liAux1)) / 4 + (367 * (udtTime.iMonth _
                    - 2 - 12 * liAux1)) / 12 - (3 * ((udtTime.iYear + 4900 _
                + liAux1) / 100)) / 4 + udtTime.iDay - 32075
        dJulianDate = liAux2 - 0.5 + dDecimalHours / 24.0
        ' Calculate difference between current Julian Day and JD 2451545.0 
        dElapsedJulianDays = dJulianDate - 2451545.0

        'Calculate ecliptic coordinates (ecliptic longitude and obliquity of the 
        'ecliptic in radians but without limiting the angle to be less than 2*Pi 
        '(i.e., the result may be greater than 2*Pi)
        Dim dMeanLongitude As Double
        Dim dMeanAnomaly As Double
        Dim dOmega As Double
        dOmega = 2.1429 - 0.0010394594 * dElapsedJulianDays
        dMeanLongitude = 4.895063 + 0.017202791698 * dElapsedJulianDays   'Radians
        dMeanAnomaly = 6.24006 + 0.0172019699 * dElapsedJulianDays
        dEclipticLongitude = dMeanLongitude + 0.03341607 * Math.Sin(dMeanAnomaly) _
            + 0.00034894 * Math.Sin(2 * dMeanAnomaly) - 0.0001134 _
            - 0.0000203 * Math.Sin(dOmega)
        dEclipticObliquity = 0.4090928 - 0.000000006214 * dElapsedJulianDays _
            + 0.0000396 * Math.Cos(dOmega)


        ' Calculate celestial coordinates ( right ascension and declination ) in radians 
        'but without limiting the angle to be less than 2*Pi (i.e., the result may be 
        ' greater than 2*Pi)
        Dim dSin_EclipticLongitude As Double
        dSin_EclipticLongitude = Math.Sin(dEclipticLongitude)
        dY = Math.Cos(dEclipticObliquity) * dSin_EclipticLongitude
        dX = Math.Cos(dEclipticLongitude)
        dRightAscension = Math.Atan2(dY, dX)
        If (dRightAscension < 0.0) Then dRightAscension = dRightAscension + twopi
        dDeclination = Math.Asin(Math.Sin(dEclipticObliquity) * dSin_EclipticLongitude)


        ' Calculate local coordinates ( azimuth and zenith angle ) in degrees
        Dim dGreenwichMeanSiderealTime As Double
        Dim dLocalMeanSiderealTime As Double
        Dim dLatitudeInRadians As Double
        Dim dHourAngle As Double
        Dim dCos_Latitude As Double
        Dim dSin_Latitude As Double
        Dim dCos_HourAngle As Double
        Dim dParallax As Double
        dGreenwichMeanSiderealTime = 6.6974243242 + _
            0.0657098283 * dElapsedJulianDays _
            + dDecimalHours
        dLocalMeanSiderealTime = (dGreenwichMeanSiderealTime * 15 _
            + udtLocation.dLongitude) * rad
        dHourAngle = dLocalMeanSiderealTime - dRightAscension
        dLatitudeInRadians = udtLocation.dLatitude * rad
        dCos_Latitude = Math.Cos(dLatitudeInRadians)
        dSin_Latitude = Math.Sin(dLatitudeInRadians)
        dCos_HourAngle = Math.Cos(dHourAngle)
        sunpos.dZenithAngle = (Math.Acos(dCos_Latitude * dCos_HourAngle _
            * Math.Cos(dDeclination) + Math.Sin(dDeclination) * dSin_Latitude))
        dY = -Math.Sin(dHourAngle)
        dX = Math.Tan(dDeclination) * dCos_Latitude - dSin_Latitude * dCos_HourAngle
        sunpos.dAzimuth = Math.Atan2(dY, dX)
        If (sunpos.dAzimuth < 0.0) Then
            sunpos.dAzimuth = sunpos.dAzimuth + twopi
        End If
        sunpos.dAzimuth = sunpos.dAzimuth / rad
        ' Parallax Correction
        dParallax = (dEarthMeanRadius / dAstronomicalUnit) _
            * Math.Sin(sunpos.dZenithAngle)
        sunpos.dZenithAngle = (sunpos.dZenithAngle _
            + dParallax) / rad

    End Function
    Structure cTime
        Public iYear As Integer
        Public iMonth As Integer
        Public iDay As Integer
        Public dHours As Double
        Public dMinutes As Double
        Public dSeconds As Double
    End Structure
    Structure cLocation
        Public dLongitude As Double
        Public dLatitude As Double
    End Structure
    Structure cSunCoordinates
        Public dZenithAngle As Double
        Public dAzimuth As Double
    End Structure

    'translate Spherical Coordinate System (Azimuth and Zenith) to Cartesian (XYZ)
    Function SunposXYZ() As cSunXYZ
        'http://ch.mathworks.com/help/matlab/ref/sph2cart.html?requestedDomain=www.mathworks.com
        Dim r As Double = 1
        'don't know, why I have to do this 90°- stuff and especially why I have to make *-1 for X...
        SunposXYZ.x = (r * Math.Cos(rad * 90 - rad * udtCoordinates.dZenithAngle) * Math.Cos((rad * udtCoordinates.dAzimuth) + (rad * 90))) * -1
        SunposXYZ.y = r * Math.Cos(rad * 90 - rad * udtCoordinates.dZenithAngle) * Math.Sin((rad * udtCoordinates.dAzimuth) + (rad * 90))
        SunposXYZ.z = r * Math.Sin(rad * 90 - rad * udtCoordinates.dZenithAngle)
    End Function

    Structure cSunXYZ
        Public x As Double
        Public y As Double
        Public z As Double
    End Structure
End Class
