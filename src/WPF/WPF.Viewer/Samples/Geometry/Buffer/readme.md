# Buffer

Create a buffer around a map point and display the results as a `Graphic`

![Image of Buffer](Buffer.jpg)

## Use case

Creating buffers is a core concept in GIS proximity analysis that allows you to visualize and locate geographic features contained within a polygon. For example, suppose you wanted to visualize areas of your city where alcohol sales are prohibited because they are within 500 meters of a school. The first step in this proximity analysis would be to generate 500 meter buffer polygons around all schools in the city. Any such businesses you find inside one of the resulting polygons are violating the law.

## How to use the sample

1. Tap on the map.
2. A planar and a geodesic buffer will be created at the tap location using the distance (miles) specified in the text box.
3. Continue tapping to create additional buffers. Notice that buffers closer to the equator appear similar in size. As you move north or south from the equator, however, the geodesic polygons become much larger. Geodesic polygons are in fact a better representation of the true shape and size of the buffer. Geodesic buffers will not be generated for points placed beyond +/-90 degrees latitude.
4. Click `Clear` to remove all buffers and start again.

## How it works

1. The `MapPoint` for a tap on the display is captured.
2. The static extension method `GeometryEngine.Buffer` is called to create a planar buffer polygon from the map location and distance.
3. Another static extension method, `GeometryEngine.BufferGeodetic` is called to create a geodesic buffer polygon using the same inputs.
4. The polygon results (and tap location) are displayed in the map view with different symbols in order to highlight the difference between the buffer techniques due to the spatial reference used in the planar calculation.

## Relevant API

* GeometryEngine.Buffer
* GeometryEngine.BufferGeodetic
* GraphicsOverlay

## Additional information

Buffers can be generated as either `planar` (flat - coordinate space of the map's spatial reference) or `geodesic` (technique that considers the curved shape of the Earth's surface, which is generally a more accurate representation). In general, distortion in the map increases as you move away from the standard parallels of the spatial reference's projection. This map is in Web Mercator so areas near the equator are the most accurate. As you move the buffer location north or south from that line, you'll see a greater difference in the polygon size and shape. Planar operations are generally faster, but performance improvement may only be noticeable for large operations (buffering a great number or complex geometry).

Geodesic buffers in the far northern and southern regions of the map will extend beyond the map's limits. The visible extent of the basemap in this sample is limited to between approximately +/-85 degrees latitude while geodesic buffers are calculated to extend all the way to the poles (+/-90 degrees). Also, since map view wraparound is active, geodesic buffers that cross the international date line (180 degrees longitude) will be [normalized](https://developers.arcgis.com/net/api-reference/api/net/Esri.ArcGISRuntime/Esri.ArcGISRuntime.Geometry.GeometryEngine.NormalizeCentralMeridian.html), resulting in a multipart geometry. This results in a vertical line in the buffer graphic at the dateline.

For more information about using buffer analysis, see the topic [How Buffer (Analysis) works](https://pro.arcgis.com/en/pro-app/tool-reference/analysis/how-buffer-analysis-works.htm) in the *ArcGIS Pro* documentation.  

## Tags

analysis, buffer, euclidean, geodesic, geometry, planar
