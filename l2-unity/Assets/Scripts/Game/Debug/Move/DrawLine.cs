using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class DrawLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<LineModel> lineRenderers = new List<LineModel>(); 
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Update()
    {

    }


    public void DrawNewLineV2(int entity_id , Vector3 point1, Vector3 point2 , Color color)
    {

        LineRenderer newLine = Instantiate(lineRenderer);
        newLine.positionCount = 2;

        // Задаем цвета линии
        newLine.startColor = color;
        newLine.endColor = color;

        // Задаем ширину линии
        newLine.startWidth = 0.01f;
        newLine.endWidth = 0.01f;

        if (newLine.material != null)
        {
            newLine.material.color = color;
        }

        // Устанавливаем позиции
        newLine.SetPosition(0, point1);
        newLine.SetPosition(1, point2);

        // Добавляем новую линию в список
        lineRenderers.Add(new LineModel(entity_id , newLine , point1 , point2));
    }

    public void RemoveDebugLines(int entity_id)
    {
        List<LineModel> list = lineRenderers.FindAll(line => line.GetEntityId() == entity_id);

        for(int i=0; i< list.Count; i++)
        {
            LineModel model = list[i];
            Destroy(model.GetLineRender());
            lineRenderers.Remove(model);
        }
       
    }

    public class LineModel
    {
        private LineRenderer render;
        private Vector3 point1;
        private Vector3 point2;
        private int entity_id;

        public LineModel(int entity_id , LineRenderer render, Vector3 point1, Vector3 point2)
        {
            this.render = render;
            this.point1 = point1;
            this.point2 = point2;
            this.entity_id = entity_id;
        }

        public int GetEntityId()
        {
            return entity_id;
        }

        public LineRenderer GetLineRender()
        {
            return render;
        }

        public override bool Equals(object obj)
        {
            if (obj is LineModel other)
            {

                return entity_id == other.entity_id;
            }
            return false;
        }
     
    }


}
