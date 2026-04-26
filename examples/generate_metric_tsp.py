import argparse
import math
import random
import os

def generate_metric_tsp_matrix(num_vertices, max_coord):
    points = [(random.randint(0, max_coord), random.randint(0, max_coord)) for _ in range(num_vertices)]
    matrix = [[0] * num_vertices for _ in range(num_vertices)]
    
    for i in range(num_vertices):
        for j in range(num_vertices):
            if i != j:
                dx = points[i][0] - points[j][0]
                dy = points[i][1] - points[j][1]
                dist = math.ceil(math.sqrt(dx**2 + dy**2))
                matrix[i][j] = max(1, int(dist))
                
    for k in range(num_vertices):
        for i in range(num_vertices):
            for j in range(num_vertices):
                if i != j and j != k and i != k:
                    if matrix[i][j] > matrix[i][k] + matrix[k][j]:
                        matrix[i][j] = matrix[i][k] + matrix[k][j]
                        
    return matrix

def verify_triangle_inequality(matrix):
    n = len(matrix)
    for i in range(n):
        for j in range(n):
            for k in range(n):
                if i != j and j != k and i != k:
                    if matrix[i][j] > matrix[i][k] + matrix[k][j]:
                        print(f"ERROR: Triangle inequality violated for vertices {i}, {j}, {k}!")
                        print(f"d({i},{j})={matrix[i][j]} > d({i},{k})={matrix[i][k]} + d({k},{j})={matrix[k][j]}")
                        return False
    return True

def save_to_file(matrix, filepath):
    num_vertices = len(matrix)
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(f"{num_vertices}\n")
        for row in matrix:
            f.write(" ".join(map(str, row)) + "\n")

def main():
    parser = argparse.ArgumentParser(description="Metric TSP instance generator")
    parser.add_argument("--start", type=int, required=True, help="Starting number of vertices")
    parser.add_argument("--step", type=int, required=True, help="Step size for increasing vertices")
    parser.add_argument("-c", "--count", type=int, required=True, help="Number of files to generate")
    parser.add_argument("-s", "--size", type=int, default=1000, help="Maximum coordinate value (default: 1000)")
    parser.add_argument("-d", "--dir", type=str, default="data", help="Target directory for files (default: 'data')")
    
    args = parser.parse_args()
    
    if not os.path.exists(args.dir):
        os.makedirs(args.dir)
        
    print(f"Generating {args.count} instances starting from {args.start} vertices with a step of {args.step}...")
    
    for i in range(args.count):
        current_vertices = args.start + i * args.step
        
        filename = f"gen_n{current_vertices:04d}.txt"
        filepath = os.path.join(args.dir, filename)
        
        matrix = generate_metric_tsp_matrix(current_vertices, args.size)
        
        if not verify_triangle_inequality(matrix):
            print(f"Skipping {filename} due to triangle inequality violation.")
            continue

        save_to_file(matrix, filepath)
        print(f"Saved instance: {filepath}")
        
    print("File generation completed.")

if __name__ == "__main__":
    main()