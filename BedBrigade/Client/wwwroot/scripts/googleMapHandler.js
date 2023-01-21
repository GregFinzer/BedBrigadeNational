window.googleMaps =
{
    origin: document.getElementById("origin"),
    Marks: ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'],
    startFrom: '',
    currentPos: '',
    endTrip: '',
    facilityName: '',
    wayPoints: '',
    IsRoute: '',
    destination: '',
    setParameters: function (facilityName, waypoints, isRoute, destination) {
        facilityName = facilityName;
        wayPoints = waypoints;
        IsRoute = isRoute;
        this.destination = destination;
    },
    checkGeoLocation: function checkGeoLocation() {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(function (position) {
                endTrip = destination;
                googleMaps.ReverseGeo(position);
            });
        } else {
            origin.innerText = 'Unable to get your current location';
        }
    },
    myMap: function myMap(position) {
        var directionsService = new google.maps.DirectionsService;
        var directionsDisplay = new google.maps.DirectionsRenderer;
        var mapCanvas = document.getElementById("map");
        var mapOptions = {
            center: new google.maps.LatLng(position.coords.latitude, position.coords.longitude),
            zoom: 16
        };
        var map = new google.maps.Map(mapCanvas, mapOptions);
        directionsDisplay.setMap(map);
        directionsDisplay.setPanel(document.getElementById('directionsPanel'));
        directionsService.route({
            origin: startFrom,
            destination: endTrip,
            waypoints: wayPoints,
            optimizeWaypoints: true,
            travelMode: 'DRIVING'
        },
            function (response, status) {
                if (status === 'OK') {
                    directionsDisplay.setDirections(response);
                    var route = response.routes[0];
                    var summaryPanel = document.getElementById('directions');
                    var miles = 0.0;
                    summaryPanel.innerHTML = '<strong>Directions:</strong><br/>';
                    // For each route, display summary information.
                    for (var i = 0; i < route.legs.length; i++) {
                        var routeSegment;
                        if (route.legs.length > 1 && i === 0) {
                            routeSegment = Marks[route.legs.length];
                        }
                        else {
                            routeSegment = Marks[i];
                        }
                        routeSegment += ' to ' + Marks[i + 1];

                        if (facilityName == null || facilityName == '') {
                            routeSegment += ' (' + facilityName[i] + ')';
                        }
                        else {
                            routeSegment += ' (' + faciltyName + ')';
                        }
                        //@if (string.IsNullOrEmpty(Model.FacilityName)) {
                        //    @Html.Raw(" +  ' (' + facilityName[i] + ')';")
                        //}
                        //else {
                        //    @Html.Raw("+ \" (" + Model.FacilityName + ")\";")
                        //}
                        summaryPanel.innerHTML += '<b>Route Segment: ' + routeSegment + '</b><br>';
                        summaryPanel.innerHTML += route.legs[i].start_address + ' ==> ';
                        summaryPanel.innerHTML += route.legs[i].end_address + '<br>';
                        summaryPanel.innerHTML += route.legs[i].distance.text + '<br><br>';
                        miles += parseFloat(route.legs[i].distance.text);

                    }
                    if (route.legs.length === 1) {
                        miles += miles;
                    }
                    summaryPanel.innerHTML += '<strong>Total route: ' +
                        miles.toFixed(1) +
                        ' mi (with return to origin).</strong>';
                } else {
                    window.alert('Directions request failed due to ' + status);
                }
            })
    },
    ReverseGeo: function ReverseGeo(position) {
        var geocoder = new google.maps.Geocoder;
        latlng = { lat: position.coords.latitude, lng: position.coords.longitude };
        geocoder.geocode({ 'location': latlng },
            function (results, status) {
                if (status === 'OK') {
                    if (results[0].formatted_address != null) {
                        var origin = document.getElementById("origin");
                        origin.innerText = 'Starting From: ' + results[0].formatted_address;
                        startFrom = results[0].formatted_address;
                        if (IsRoute) {
                            endTrip = startFrom;
                        }
                        //@if (Model.IsRoute) {
                        //    @Html.Raw("endTrip = startFrom;");
                        //}
                        facilityName.push(startFrom);
                        myMap(position);
                    } else {
                        alert('No results found');
                    }
                } else {
                    alert('Geocoder failed due to: ' + status);
                }
            });
    }
};