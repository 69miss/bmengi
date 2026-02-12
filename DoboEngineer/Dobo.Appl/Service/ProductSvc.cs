using Dobo.Appl.Entity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dobo.Appl.Service
{
    public class ProductSvc : ServiceBase<Product>
    {
        /// <summary>
        /// 获取默认产品
        /// </summary>
        /// <returns></returns>
        public Product DefProduct()
        {
            var re = fsql.Select<Product>().Where(p => p.Name == "默认产品").First();
            if (re == null)
            {
                var newProduct = new Product() { Name = "默认产品", CreateTime = DateTimeOffset.Now, Types = "L|a*|b*|E" };
                newProduct.Y0Position = "[50,0,0,0]";
                newProduct.ViewCfgObj = new ViewInfo[] {
                new ViewInfo(){ Title="L", YScope=50, RangeColor=[[200, 200, 200],[50, 50, 50]] },
                new ViewInfo(){ Title="a*", YScope=10, RangeColor=[[200, 30, 30],[30, 180, 30]] },
                new ViewInfo(){ Title="b*", YScope=10, RangeColor=[[200, 170, 30],[30, 100, 200]] },
                new ViewInfo(){ Title="ΔE", YScope=-1, RangeColor=[[200, 200, 200],[30, 30, 30]] },
                };
                Add(newProduct);
            }
            return re = fsql.Select<Product>().Where(p => p.Name == "默认产品").First();
        }
        public override int Update(Product entity)
        {
            //var viewInfo= entity.ViewCfgObj;
            //viewInfo[1].RangeColor[0] = [200, 30, 30];
            //viewInfo[1].RangeColor[1] = [30, 180, 30];
            //viewInfo[2].RangeColor[0] = [200, 170, 30];
            //viewInfo[2].RangeColor[1] = [30, 100, 200];
            //entity.ViewCfgObj=viewInfo;
            return base.Update(entity);
        }
    }
}
