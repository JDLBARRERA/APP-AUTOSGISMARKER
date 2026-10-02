using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARTrackBuilder.Data
{
    public enum ProductCategory
    {
        Tapete,
        Autos,
        Gises
    }

    [Serializable]
    public struct ProductItem
    {
        public string ProductID;
        public string DisplayName;
        public ProductCategory Category;
        public string Description;
        public float Price;
        public Sprite ProductIcon;
        [Tooltip("El enlace a la tienda web (Shopify/Amazon) para envío a casa.")]
        public string PurchaseURL; 
    }

    /// <summary>
    /// Gestiona el inventario del escaparate virtual. En un producto final, 
    /// esta lista se descargaría de un servidor en la nube.
    /// </summary>
    public class StoreDataManager : MonoBehaviour
    {
        [Header("Inventario de la Tienda")]
        [SerializeField] private List<ProductItem> _catalog = new List<ProductItem>();

        public List<ProductItem> GetProductsByCategory(ProductCategory category)
        {
            return _catalog.FindAll(p => p.Category == category);
        }

        public List<ProductItem> GetAllProducts()
        {
            return _catalog;
        }
    }
}
