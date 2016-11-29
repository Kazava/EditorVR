﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Tools;
using UnityEngine.VR.Utilities;
using UnityEngine.VR.Workspaces;

public class HierarchyWorkspace : Workspace, IFilterUI, IConnectInterfaces
{
	const float kYBounds = 0.2f;
	const float kScrollMargin = 0.03f;

	[SerializeField]
	GameObject m_ContentPrefab;

	[SerializeField]
	GameObject m_FilterPrefab;

	HierarchyUI m_HierarchyUI;
	FilterUI m_FilterUI;

	Vector3 m_ScrollStart;
	float m_ScrollOffsetStart;
	HierarchyData m_SelectedRow;

	bool m_HierarchyPanelDragging;
	Transform m_HierarchyPanelHighlightContainer;

	public ConnectInterfacesDelegate connectInterfaces { get; set; }

	public HierarchyData[] hierarchyData
	{
		set
		{
			m_HierarchyData = value;

			if (m_HierarchyUI)
				m_HierarchyUI.hierarchyListView.data = value;
		}
	}
	HierarchyData[] m_HierarchyData;

	public List<string> filterList { set { m_FilterUI.filterList = value; } }

	public override void Setup()
	{
		// Initial bounds must be set before the base.Setup() is called
		minBounds = new Vector3(kMinBounds.x, kMinBounds.y, 0.5f);
		m_CustomStartingBounds = minBounds;

		base.Setup();

		topPanelDividerOffset = -0.2875f; // enable & position the top-divider(mask) slightly to the left of workspace center

		var contentPrefab = U.Object.Instantiate(m_ContentPrefab, m_WorkspaceUI.sceneContainer, false);
		m_HierarchyUI = contentPrefab.GetComponent<HierarchyUI>();
		hierarchyData = m_HierarchyData;

		m_FilterUI = U.Object.Instantiate(m_FilterPrefab, m_WorkspaceUI.frontPanel, false).GetComponent<FilterUI>();

		var hierarchyListView = m_HierarchyUI.hierarchyListView;
		hierarchyListView.selectRow = SelectRow;

		var handle = m_HierarchyUI.scrollHandle;
		// Scroll Handle shouldn't move on bounds change
		handle.transform.parent = m_WorkspaceUI.sceneContainer;

		handle.dragStarted += OnScrollDragStarted;
		handle.dragging += OnScrollDragging;
		handle.dragEnded += OnScrollDragEnded;

		// Hookup highlighting calls
		handle.dragStarted += OnScrollPanelDragHighlightBegin;
		handle.dragEnded += OnScrollPanelDragHighlightEnd;
		handle.hoverStarted += OnScrollPanelHoverHighlightBegin;
		handle.hoverEnded += OnScrollPanelHoverHighlightEnd;

		// Assign highlight references
		m_HierarchyPanelHighlightContainer = m_HierarchyUI.highlight.transform.parent.transform;

		// Propagate initial bounds
		OnBoundsChanged();
	}

	protected override void OnBoundsChanged()
	{
		const float depthCompensation = 0.1375f;

		var bounds = contentBounds;
		var size = bounds.size;
		size.y = kYBounds;
		size.z = size.z - depthCompensation;
		bounds.size = size;
		bounds.center = Vector3.zero;

		const float kHalfScrollMargin = kScrollMargin * 0.5f;
		const float kDoubleScrollMargin = kScrollMargin * 2;
		const float kScrollHandleXPositionOffset = 0.025f;
		const float kScrollHandleXScaleOffset = 0.015f;

		var scrollHandleTransform = m_HierarchyUI.scrollHandle.transform;
		scrollHandleTransform.localPosition = new Vector3(-kHalfScrollMargin + kScrollHandleXPositionOffset, -scrollHandleTransform.localScale.y * 0.5f, 0);
		scrollHandleTransform.localScale = new Vector3(size.x + kScrollMargin + kScrollHandleXScaleOffset, scrollHandleTransform.localScale.y, size.z + kDoubleScrollMargin);

		var listView = m_HierarchyUI.hierarchyListView;
		bounds.size = size;
		listView.bounds = bounds;
		listView.PreCompute(); // Compute item size
		listView.transform.localPosition = new Vector3(0, listView.itemSize.y * 0.5f, 0); // Center in Y

		m_HierarchyPanelHighlightContainer.localScale = new Vector3(size.x, 1f, size.z);

		size = contentBounds.size;
		size.z = size.z - depthCompensation;
		bounds.size = size;
	}

	static void SelectRow(int instanceID)
	{
		var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
		if (gameObject)
			Selection.activeGameObject = gameObject;
	}

	void OnScrollDragStarted(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_ScrollStart = eventData.rayOrigin.transform.position;
		m_ScrollOffsetStart = m_HierarchyUI.hierarchyListView.scrollOffset;
		m_HierarchyUI.hierarchyListView.OnBeginScrolling();
	}

	void OnScrollDragging(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
	}

	void OnScrollDragEnded(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		Scroll(handle, eventData);
		m_ScrollOffsetStart = m_HierarchyUI.hierarchyListView.scrollOffset;
		m_HierarchyUI.hierarchyListView.OnScrollEnded();
	}

	void Scroll(BaseHandle handle, HandleEventData eventData)
	{
		var scrollOffset = m_ScrollOffsetStart + Vector3.Dot(m_ScrollStart - eventData.rayOrigin.transform.position, transform.forward);
		m_HierarchyUI.hierarchyListView.scrollOffset = scrollOffset;
	}

	void OnScrollPanelDragHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_HierarchyPanelDragging = true;
		m_HierarchyUI.highlight.visible = true;
	}

	void OnScrollPanelDragHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_HierarchyPanelDragging = false;
		m_HierarchyUI.highlight.visible = false;
	}

	void OnScrollPanelHoverHighlightBegin(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		m_HierarchyUI.highlight.visible = true;
	}

	void OnScrollPanelHoverHighlightEnd(BaseHandle handle, HandleEventData eventData = default(HandleEventData))
	{
		if (!m_HierarchyPanelDragging)
			m_HierarchyUI.highlight.visible = false;
	}

	bool TestFilter(string type)
	{
		return FilterUI.TestFilter(m_FilterUI.searchQuery, type);
	}
}