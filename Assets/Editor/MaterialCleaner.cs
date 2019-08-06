/*
unity-material-cleaner

Copyright (c) 2019 ina-amagami (ina@amagamina.jp)

This software is released under the MIT License.
https://opensource.org/licenses/mit-license.php
*/

using UnityEngine;
using UnityEditor;

/// <summary>
/// マテリアルお掃除ツール
/// </summary>
public class MaterialCleaner
{
	private const string MenuItemName = "Assets/Material Cleaner";

	/// <summary>
	/// Assetsメニューからの実行
	/// </summary>
	[MenuItem(MenuItemName, false)]
	public static void MaterialCleanerMenu()
	{
		if (!IsEnabledMaterialCleanerMenu())
		{
			return;
		}
		const string progressBarTitle = "[MaterialCleaner] Delete Unused Properties";

		int assetCount = Selection.assetGUIDs.Length;
		for (int i = 0; i < assetCount; ++i)
		{
			string guid = Selection.assetGUIDs[i];
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var mat = AssetDatabase.LoadMainAssetAtPath(path) as Material;
			if (mat == null)
			{
				continue;
			}
			EditorUtility.DisplayProgressBar(progressBarTitle, string.Format("{0}", mat.name), i / (float)assetCount);
			DeleteUnusedProperties(mat, path);
		}

		EditorUtility.DisplayProgressBar(progressBarTitle, "[MaterialCleaner] Refresh AssetDatabase...", 1f);
		AssetDatabase.Refresh();
		EditorUtility.ClearProgressBar();
	}
	[MenuItem(MenuItemName, true)]
	static bool IsEnabledMaterialCleanerMenu()
	{
		if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// マテリアルの不要なプロパティを削除する
	/// </summary>
	/// <param name="mat">対象のマテリアル</param>
	/// <param name="path">マテリアルが保存されているパス</param>
	public static void DeleteUnusedProperties(Material mat, string path)
	{
		// 不要なパラメータを削除する方法として、新しいマテリアルを作ってそこに必要なパラメータだけコピーする
		var newMat = new Material(mat.shader);

		// パラメータのコピー
		newMat.name = mat.name;
		newMat.renderQueue = (mat.shader.renderQueue == mat.renderQueue) ? -1 : mat.renderQueue;
		newMat.enableInstancing = mat.enableInstancing;
		newMat.doubleSidedGI = mat.doubleSidedGI;
		newMat.globalIlluminationFlags = mat.globalIlluminationFlags;
		newMat.hideFlags = mat.hideFlags;
		newMat.shaderKeywords = mat.shaderKeywords;

		// Propertiesのコピー
		var properties = MaterialEditor.GetMaterialProperties(new Material[] { mat });
		for (int pIdx = 0; pIdx < properties.Length; ++pIdx)
		{
			SetPropertyToMaterial(newMat, properties[pIdx]);
		}

		// GUIDが変わらないように置き換える
		string tempPath = path + "_temp";
		AssetDatabase.CreateAsset(newMat, tempPath);
		FileUtil.ReplaceFile(tempPath, path);
		AssetDatabase.DeleteAsset(tempPath);
	}

	/// <summary>
	/// MaterialPropertyを解釈してMaterialに設定する
	/// </summary>
	/// <param name="mat">対象のマテリアル</param>
	/// <param name="property">設定するプロパティ</param>
	public static void SetPropertyToMaterial(Material mat, MaterialProperty property)
	{
		switch (property.type)
		{
			case MaterialProperty.PropType.Color:
				mat.SetColor(property.name, property.colorValue);
				break;

			case MaterialProperty.PropType.Float:
			case MaterialProperty.PropType.Range:
				mat.SetFloat(property.name, property.floatValue);
				break;

			case MaterialProperty.PropType.Texture:
				// PerRenderDataの場合はnullになるようにする
				Texture tex = null;
				if ((property.flags & MaterialProperty.PropFlags.PerRendererData) != MaterialProperty.PropFlags.PerRendererData)
				{
					tex = property.textureValue;
				}
				mat.SetTexture(property.name, tex);
				if ((property.flags & MaterialProperty.PropFlags.NoScaleOffset) != MaterialProperty.PropFlags.NoScaleOffset)
				{
					mat.SetTextureScale(property.name, new Vector2(property.textureScaleAndOffset.x, property.textureScaleAndOffset.y));
					mat.SetTextureOffset(property.name, new Vector2(property.textureScaleAndOffset.z, property.textureScaleAndOffset.w));
				}
				break;

			case MaterialProperty.PropType.Vector:
				mat.SetVector(property.name, property.vectorValue);
				break;
		}
	}
}