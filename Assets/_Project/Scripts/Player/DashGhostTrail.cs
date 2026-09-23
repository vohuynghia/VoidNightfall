using System.Collections;
using UnityEngine;

public class DashGhostTrail : MonoBehaviour
{
	[Header("Ghost Settings")]
	[SerializeField] private Material _ghostMaterial;       // Material phát sáng t?o ? B??c 1
	[SerializeField] private float _ghostInterval = 0.04f;   // Kho?ng cách th?i gian sinh m?i bóng ma
	[SerializeField] private float _ghostLifetime = 0.35f;   // Th?i gian t?n t?i c?a bóng ma
	[SerializeField] private Color _ghostColor = new Color(0f, 0.8f, 1f, 0.7f); // Màu bóng (RGBA)

	private SkinnedMeshRenderer[] _skinnedMeshRenderers;
	private MeshRenderer[] _meshRenderers;
	private Coroutine _ghostCoroutine;

	private void Awake()
	{
		// L?y toàn b? renderers trong mô hình nhân v?t
		_skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
		_meshRenderers = GetComponentsInChildren<MeshRenderer>();
	}

	/// <summary>
	/// B?t ??u t?o chu?i bóng ma
	/// </summary>
	public void StartGhostTrail()
	{
		if (_ghostCoroutine != null)
			StopCoroutine(_ghostCoroutine);

		_ghostCoroutine = StartCoroutine(GenerateGhostRoutine());
	}

	/// <summary>
	/// D?ng t?o bóng ma
	/// </summary>
	public void StopGhostTrail()
	{
		if (_ghostCoroutine != null)
		{
			StopCoroutine(_ghostCoroutine);
			_ghostCoroutine = null;
		}
	}

	private IEnumerator GenerateGhostRoutine()
	{
		while (true)
		{
			CreateGhostSnapshot();
			yield return new WaitForSeconds(_ghostInterval);
		}
	}

	private void CreateGhostSnapshot()
	{
		// 1. X? lý nhân v?t có g?n x??ng c? ??ng (SkinnedMeshRenderer)
		if (_skinnedMeshRenderers != null && _skinnedMeshRenderers.Length > 0)
		{
			foreach (var smr in _skinnedMeshRenderers)
			{
				if (!smr.gameObject.activeInHierarchy) continue;

				GameObject ghostObj = new GameObject("Ghost_Snapshot");
				ghostObj.transform.position = smr.transform.position;
				ghostObj.transform.rotation = smr.transform.rotation;
				ghostObj.transform.localScale = smr.transform.lossyScale;

				MeshFilter mf = ghostObj.AddComponent<MeshFilter>();
				MeshRenderer mr = ghostObj.AddComponent<MeshRenderer>();

				Mesh bakedMesh = new Mesh();
				smr.BakeMesh(bakedMesh); // Ch?p l?i t? th? khung x??ng chính xác t?i frame hi?n t?i
				mf.mesh = bakedMesh;

				mr.material = _ghostMaterial;
				mr.material.color = _ghostColor;

				StartCoroutine(FadeAndDestroy(ghostObj, mr, bakedMesh));
			}
		}
		// 2. X? lý nhân v?t d?ng kh?i t?nh ho?c ph? ki?n g?n ngoài (MeshRenderer thông th??ng)
		else if (_meshRenderers != null && _meshRenderers.Length > 0)
		{
			foreach (var mrItem in _meshRenderers)
			{
				if (!mrItem.gameObject.activeInHierarchy) continue;
				MeshFilter originalFilter = mrItem.GetComponent<MeshFilter>();
				if (originalFilter == null) continue;

				GameObject ghostObj = new GameObject("Ghost_Snapshot");
				ghostObj.transform.position = mrItem.transform.position;
				ghostObj.transform.rotation = mrItem.transform.rotation;
				ghostObj.transform.localScale = mrItem.transform.lossyScale;

				MeshFilter mf = ghostObj.AddComponent<MeshFilter>();
				MeshRenderer mr = ghostObj.AddComponent<MeshRenderer>();

				mf.mesh = originalFilter.sharedMesh;
				mr.material = _ghostMaterial;
				mr.material.color = _ghostColor;

				StartCoroutine(FadeAndDestroy(ghostObj, mr, null));
			}
		}
	}

	private IEnumerator FadeAndDestroy(GameObject ghostObj, MeshRenderer mr, Mesh bakedMesh)
	{
		float timer = _ghostLifetime;
		Color initialColor = mr.material.color;

		while (timer > 0)
		{
			timer -= Time.deltaTime;
			float alpha = Mathf.Lerp(0, initialColor.a, timer / _ghostLifetime);
			mr.material.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
			yield return null;
		}

		// D?n d?p b? nh? Mesh ?ã bake ?? tránh rò r? RAM (Memory Leak)
		if (bakedMesh != null)
			Destroy(bakedMesh);

		Destroy(ghostObj);
	}
}