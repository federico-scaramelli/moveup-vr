﻿using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class QuadTreeCamMov : MonoBehaviour
{
    SimulationStatePattern _statePattern;
    Camera _referenceCamera;
    AbstractMap _map;

    [Range(0.1f, 50f)]
    public float _panSpeed = 0.3f;
    float _zoomSpeed = 0.25f;

    [SerializeField]
    bool _useDegreeMethod;

    private Vector3 _origin;
    private Vector3 _mousePosition;
    private Vector3 _mousePositionPrevious;
    private bool _shouldDrag;
    private bool _isInitialized = false;
    private Plane _groundPlane = new Plane(Vector3.up, 0);
    private bool _dragStartedOnUI = false;

    void Awake()
    {
        _statePattern = FindObjectOfType<SimulationStatePattern>();
        _referenceCamera = _statePattern.MainCam;
        _map = _statePattern.Map;

        //_panSpeed = _statePattern.DirPanSpeed / 50;
        _zoomSpeed = _statePattern.ZoomSpeed / 100;

        _map.OnInitialized += () =>
        {
            _isInitialized = true;
        };
    }

    public void Update()
    {
        if (!_statePattern.LoadingPanel.activeInHierarchy && !_statePattern.WearVisorPanel.activeInHierarchy && !_statePattern.SVNotFoundPanel.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
            {
                _dragStartedOnUI = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _dragStartedOnUI = false;
            }
        }
    }


    private void LateUpdate()
    {
        if (!_isInitialized) { return; }
        if (/*!_statePattern.LoadingPanel.activeInHierarchy && */!_statePattern.WearVisorPanel.activeInHierarchy && !_statePattern.SVNotFoundPanel.activeInHierarchy)
        {
            if (!_dragStartedOnUI)
            {
                if (Input.touchSupported && Input.touchCount > 0)
                {
                    HandleTouch();
                }
                else
                {
                    HandleMouseAndKeyBoard();
                }
            }
        }
    }

    void HandleMouseAndKeyBoard()
    {
        // zoom
        float scrollDelta = 0.0f;
        scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        ZoomMapUsingTouchOrMouse(scrollDelta);


        //pan keyboard
        float xMove = Input.GetAxis("Horizontal");
        float zMove = Input.GetAxis("Vertical");

        PanMapUsingKeyBoard(xMove, zMove);


        //pan mouse
        PanMapUsingTouchOrMouse();
    }

    void HandleTouch()
    {
        float zoomFactor = 0.0f;
        //pinch to zoom.
        switch (Input.touchCount)
        {
            case 1:
                {
                    PanMapUsingTouchOrMouse();
                }
                break;
            case 2:
                {
                    // Store both touches.
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    // Find the position in the previous frame of each touch.
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    // Find the magnitude of the vector (the distance) between the touches in each frame.
                    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    // Find the difference in the distances between each frame.
                    zoomFactor = 0.01f * (touchDeltaMag - prevTouchDeltaMag);
                }
                ZoomMapUsingTouchOrMouse(zoomFactor);
                break;
            default:
                break;
        }
    }

    void ZoomMapUsingTouchOrMouse(float zoomFactor)
    {
        var zoom = Mathf.Max(0.0f, Mathf.Min(_map.Zoom + zoomFactor * _zoomSpeed, 21.0f));
        if (Math.Abs(zoom - _map.Zoom) > 0.0f)
        {
            _map.UpdateMap(_map.CenterLatitudeLongitude, zoom);
        }
    }

    void PanMapUsingKeyBoard(float xMove, float zMove)
    {
        if (Math.Abs(xMove) > 0.0f || Math.Abs(zMove) > 0.0f)
        {
            // Get the number of degrees in a tile at the current zoom level.
            // Divide it by the tile width in pixels ( 256 in our case)
            // to get degrees represented by each pixel.
            // Keyboard offset is in pixels, therefore multiply the factor with the offset to move the center.
            float factor = _panSpeed * (Conversions.GetTileScaleInDegrees((float)_map.CenterLatitudeLongitude.x, _map.AbsoluteZoom));

            var latitudeLongitude = new Vector2d(_map.CenterLatitudeLongitude.x + zMove * factor * 2.0f, _map.CenterLatitudeLongitude.y + xMove * factor * 4.0f);

            _map.UpdateMap(latitudeLongitude, _map.Zoom);
        }
    }

    void PanMapUsingTouchOrMouse()
    {
        if (_useDegreeMethod)
        {
            UseDegreeConversion();
        }
        else
        {
            UseMeterConversion();
        }
    }

    void UseMeterConversion()
    {
        if (Input.GetMouseButtonUp(1))
        {
            var mousePosScreen = Input.mousePosition;
            //assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
            //http://answers.unity3d.com/answers/599100/view.html
            mousePosScreen.z = _referenceCamera.transform.localPosition.y;
            var pos = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

            var latlongDelta = _map.WorldToGeoPosition(pos);
        }

        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            var mousePosScreen = Input.mousePosition;
            //assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
            //http://answers.unity3d.com/answers/599100/view.html
            mousePosScreen.z = _referenceCamera.transform.localPosition.y;
            _mousePosition = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

            if (_shouldDrag == false)
            {
                _shouldDrag = true;
                _origin = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
            }
        }
        else
        {
            _shouldDrag = false;
        }

        if (_shouldDrag == true)
        {
            var changeFromPreviousPosition = _mousePositionPrevious - _mousePosition;
            if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.y) > 0.0f)
            {
                _mousePositionPrevious = _mousePosition;
                var offset = _origin - _mousePosition;

                if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.z) > 0.0f)
                {
                    if (null != _map)
                    {
                        float factor = _panSpeed * Conversions.GetTileScaleInMeters((float)0, _map.AbsoluteZoom) / _map.UnityTileSize;
                        var latlongDelta = Conversions.MetersToLatLon(new Vector2d(offset.x * factor, offset.z * factor));
                        var newLatLong = _map.CenterLatitudeLongitude + latlongDelta;

                        _map.UpdateMap(newLatLong, _map.Zoom);
                    }
                }
                _origin = _mousePosition;
            }
            else
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                _mousePositionPrevious = _mousePosition;
                _origin = _mousePosition;
            }
        }
    }

    void UseDegreeConversion()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            var mousePosScreen = Input.mousePosition;
            //assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
            //http://answers.unity3d.com/answers/599100/view.html
            mousePosScreen.z = _referenceCamera.transform.localPosition.y;
            _mousePosition = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

            if (_shouldDrag == false)
            {
                _shouldDrag = true;
                _origin = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
            }
        }
        else
        {
            _shouldDrag = false;
        }

        if (_shouldDrag == true)
        {
            var changeFromPreviousPosition = _mousePositionPrevious - _mousePosition;
            if (Mathf.Abs(changeFromPreviousPosition.x) > 0.0f || Mathf.Abs(changeFromPreviousPosition.y) > 0.0f)
            {
                _mousePositionPrevious = _mousePosition;
                var offset = _origin - _mousePosition;

                if (Mathf.Abs(offset.x) > 0.0f || Mathf.Abs(offset.z) > 0.0f)
                {
                    if (null != _map)
                    {
                        // Get the number of degrees in a tile at the current zoom level.
                        // Divide it by the tile width in pixels ( 256 in our case)
                        // to get degrees represented by each pixel.
                        // Mouse offset is in pixels, therefore multiply the factor with the offset to move the center.
                        float factor = _panSpeed * Conversions.GetTileScaleInDegrees((float)_map.CenterLatitudeLongitude.x, _map.AbsoluteZoom) / _map.UnityTileSize;

                        var latitudeLongitude = new Vector2d(_map.CenterLatitudeLongitude.x + offset.z * factor, _map.CenterLatitudeLongitude.y + offset.x * factor);
                        _map.UpdateMap(latitudeLongitude, _map.Zoom);
                    }
                }
                _origin = _mousePosition;
            }
            else
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                _mousePositionPrevious = _mousePosition;
                _origin = _mousePosition;
            }
        }
    }

    private Vector3 getGroundPlaneHitPoint(Ray ray)
    {
        float distance;
        if (!_groundPlane.Raycast(ray, out distance)) { return Vector3.zero; }
        return ray.GetPoint(distance);
    }
}