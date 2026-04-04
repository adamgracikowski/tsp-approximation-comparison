import pandas as pd
import matplotlib.pyplot as plt
import argparse
import os

def plot_tsp_approximation_ratio(csv_filepath, limit, output_dir):
    os.makedirs(output_dir, exist_ok=True)

    try:
        df = pd.read_csv(csv_filepath)
    except Exception as e:
        print(f"Error while reading CSV file: {e}")
        return

    required_cols = ['FileName', 'Algorithm', 'VertexCount', 'TotalDistance']
    for col in required_cols:
        if col not in df.columns:
            print(f"Error: Column '{col}' is missing in the CSV file.")
            return

    pattern = r'o(\d+)\.txt'
    opt_costs_raw = df['FileName'].str.extract(pattern, expand=False)
    
    if opt_costs_raw.isnull().any():
        print("Error: Failed to extract optimal cost from some file names.")
        return
        
    df['OptimalDistance'] = opt_costs_raw.astype(int)
    
    df['ApproxRatio'] = df['TotalDistance'] / df['OptimalDistance']

    plt.figure(figsize=(10, 6))
    grouped = df.groupby('Algorithm')

    for algorithm_name, group_data in grouped:
        group_data = group_data.sort_values(by='VertexCount')
        
        scatter = plt.scatter(
            group_data['VertexCount'], 
            group_data['ApproxRatio'], 
            label=algorithm_name, 
            alpha=0.8,
            s=50,
            zorder=3
        )
        
        base_color = scatter.get_facecolor()[0]
        
        plt.plot(
            group_data['VertexCount'], 
            group_data['ApproxRatio'], 
            color=base_color,
            alpha=0.25,
            linewidth=2,
            zorder=2
        )

    plt.axhline(
        y=limit, 
        color='r', 
        linestyle='--', 
        linewidth=2, 
        alpha=0.8,
        zorder=1
    )

    plt.xlabel('Liczba wierzchołków', fontsize=12)
    plt.ylabel('Współczynnik aproksymacji', fontsize=12)
    
    plt.legend(title='Algorytm', fontsize=10)
    plt.grid(True, linestyle='--', alpha=0.6, zorder=0)

    plt.tight_layout()

    plot_path = os.path.join(output_dir, "approximation_ratio_plot.png")
    plt.savefig(plot_path, dpi=300, bbox_inches='tight')

def main():
    parser = argparse.ArgumentParser(description="Approximation Quality plotter for TSP algorithms based on CSV results.")
    parser.add_argument("-i", "--input", type=str, required=True, help="Path to the input CSV file containing TSP results.")
    parser.add_argument("-l", "--limit", type=float, default=2.0, help="Height of the horizontal limit line (default: 2.0)")
    parser.add_argument("--out", type=str, default="plots", help="Directory to save the generated plot (default: 'plots').")
    
    args = parser.parse_args()

    if not os.path.exists(args.input):
        print(f"Error: File '{args.input}' does not exist.")
        return

    plot_tsp_approximation_ratio(args.input, args.limit, args.out)

if __name__ == "__main__":
    main()